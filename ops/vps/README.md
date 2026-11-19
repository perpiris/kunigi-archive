# VPS operations

Everything about the production server that isn't the app itself.

- **Host:** `tokinigi.gr`, Debian 13, SSH on port 22
- **Site:** https://tokinigi.gr (`src/KunigiArchive.Web`)

## Files here

| File | What it is |
|---|---|
| `setup.sh` | Idempotent host configuration (journal cap, cache prune, backup script + cron, optional SQL tuning). Safe to re-run. |
| `wireguard.sh` | VPN helper, installed as `/usr/local/sbin/kunigi-wireguard` (`init`, `add <name>`, `list`). |
| `init-secrets.sh` | Creates the env files in `/opt/kunigi` (if missing), the database, the SQL logins they use and the site's data folders. `--rotate-sa` gives `sa` a new password. Safe to re-run. |
| `rotate-smtp.sh` | Replaces the SMTP password (Mailtrap token) in `web.env` without showing it, and recreates the site container (#37). |
| `backup/vps-backup.sh` | Nightly backup script. Installed to `/opt/kunigi-backups/vps-backup.sh` by `setup.sh`. |
| `sql/tune.sql` | SQL Server settings (SIMPLE recovery, smaller diagnostic traces). Applied by `setup.sh --sql`. |

To apply after changing any of them (the deploy workflow copies the repo to `~iperp/kunigi-archive`):

```bash
ssh kunigi-vps 'sudo ~iperp/kunigi-archive/ops/vps/setup.sh --sql'
```

## Layout on the server

| What | Where |
|---|---|
| Deployed repo / compose project | `/home/iperp/kunigi-archive` (compose project `kunigi-archive`); replaced as a whole by every **Deploy web** |
| Containers | `kunigi_web` (the site, :8080), `kunigi_db` (SQL Server 2022), `nginx-proxy`, `nginx-acme-companion` |
| **Database** | Docker volume `kunigi-archive_db_data` (`/var/lib/docker/volumes/kunigi-archive_db_data/_data`), DB `KunigiArchiveV2` (the name is historical: it was the new app's database while the old site still ran) |
| **Uploaded media** | `/home/iperp/kunigi_web_media` → `/app/media` in `kunigi_web` |
| Sign-in keys | `/home/iperp/kunigi_web_keys` → `/app/keys` (Data Protection; losing them signs everyone out) |
| App logs | `/home/iperp/kunigi_web_logs` (JSON lines, a file a day, new file past 10 MB, 30 kept), plus `docker logs kunigi_web` (capped at 5 × 10 MB) |
| Old MVC site (rollback, until removed) | DB `KunigiArchive`, media `/home/iperp/kunigi_media`, image `kunigi-archive-web:old-mvc`, `/opt/kunigi/app.env`. Frozen on 2026-11-19, read by nothing. See [Rollback](#rollback-to-the-old-site). |
| Backups | `/opt/kunigi-backups/kunigi_backup_<timestamp>.tar.gz` (newest 3 kept) |
| Backup log | `/var/log/kunigi-backup.log` |
| Backup cron | root crontab, `0 2 * * *` |
| Cache prune cron | `/etc/cron.d/docker-builder-prune` (Sundays 04:00) |
| Journal cap | `/etc/systemd/journald.conf.d/size.conf` (200 MB) |

The three `kunigi_web_*` folders are owned by uid 1654, the container user.

**Never** run `docker compose down -v`, and never rename the compose project or volumes: that deletes or detaches the database.

## Secrets and SQL logins

Secrets exist **only on the VPS**, in `/opt/kunigi`. The dir is `root:iperp 750`. `db.env` and `web.env` are `root:iperp 640`, because the deploy runs `docker compose` as `iperp` and compose reads `env_file` itself. `backup.env` and `admin.env` are root-only (`600`). They are not in the repo or in GitHub, and they are included in every backup (`config/`).

| File | Used by | Contents |
|---|---|---|
| `db.env` | `kunigi_db` | `ACCEPT_EULA`, `MSSQL_SA_PASSWORD` |
| `web.env` | `kunigi_web` | `ConnectionStrings__KunigiArchive` (login `kunigi_beta`), `AdminSeed__Email`, `AdminSeed__Password` (first admin on an empty database only), `EmailSettings__Password` (Mailtrap token), `EmailSettings__ApplicationUrl` (`https://tokinigi.gr`) |
| `backup.env` | `vps-backup.sh` | `BACKUP_SQL_USER`, `BACKUP_SQL_PASSWORD` (login `kunigi_backup`) |
| `admin.env` | you (SSMS), maintenance | `SQL_ADMIN_USER`, `SQL_ADMIN_PASSWORD` (login `kunigi_admin`) |
| `app.env`, `beta.env` | nothing (old site / old beta) | kept for a rollback; delete with the old data |

| SQL login | Rights | Purpose |
|---|---|---|
| `kunigi_beta` | `db_owner` on `KunigiArchiveV2` only | the site (owner rights because migrations run at startup). The name is historical; a new server gets `kunigi_web` (`init-secrets.sh` uses whatever `web.env` names). |
| `kunigi_backup` | `db_backupoperator` on both databases + `CREATE ANY DATABASE` (needed for `RESTORE VERIFYONLY`) | nightly backup |
| `kunigi_admin` | `sysadmin` | manual administration / SSMS |
| `kunigi_app` | `db_owner` on `KunigiArchive` | the old site; unused since 2026-11-19, drop with the old data |
| `sa` | `sysadmin` | `init-secrets.sh` only (#46); rotate with `init-secrets.sh --rotate-sa` |

Any `appsettings.json` key can be overridden in `web.env` with `Section__Key=value`. After editing `web.env`, recreate the site: `cd ~iperp/kunigi-archive && sudo docker compose up -d --no-build web`.

To see a value: `ssh kunigi-vps 'sudo cat /opt/kunigi/admin.env'`.

**Recreating the database** (e.g. a restore into an empty server): dropping `KunigiArchiveV2` also drops its database users, so run `sudo bash ~iperp/kunigi-archive/ops/vps/init-secrets.sh` afterwards: it gives the site's login and `kunigi_backup` their rights again. Without it the nightly backup fails with `BACKUP FAILED`.

## Rollback to the old site

Until the old data is removed (#89), the old MVC site can be brought back exactly as it was frozen on 2026-11-19 (changes made on the new site since then don't carry back):

```bash
cd ~iperp/kunigi-archive && sudo docker compose stop web
sudo docker run -d --name kunigi_web_old --network kunigi-archive_default --env-file /opt/kunigi/app.env \
  -e VIRTUAL_HOST=tokinigi.gr,www.tokinigi.gr -e LETSENCRYPT_HOST=tokinigi.gr,www.tokinigi.gr -e VIRTUAL_PORT=8080 \
  -e ASPNETCORE_URLS=http://+:8080 -e MediaDirectory=/app/media -v /home/iperp/kunigi_media:/app/media \
  --restart always kunigi-archive-web:old-mvc
```

Back to the new site: `sudo docker rm -f kunigi_web_old && cd ~iperp/kunigi-archive && sudo docker compose up -d web`.

## VPN (WireGuard) and SSMS

SQL Server is published on port 1433, but the firewall only lets it through when the connection comes over the WireGuard VPN. From the public internet the port is blocked.

- VPN subnet `10.8.0.0/24`: the VPS is `10.8.0.1`, devices get `.2`, `.3`, …
- Server: `wg-quick@wg0`, config in `/etc/wireguard/wg0.conf`, UDP 51820 open in ufw
- Device configs: `/etc/wireguard/clients/<name>.conf`
- Devices so far: `desktop` (10.8.0.2), `laptop` (10.8.0.3)
- Firewall: the `KUNIGI DOCKER-USER` block in `/etc/ufw/after.rules` (written by `setup.sh`)

**Connect with SSMS**
1. Install WireGuard (https://www.wireguard.com/install/), choose **Import tunnel(s) from file**, pick the device's `.conf`, and click **Activate**.
2. In SSMS: server `10.8.0.1,1433`, SQL Server Authentication, user and password from `admin.env`, and tick **Trust server certificate**.

Only traffic to `10.8.0.0/24` goes through the VPN; everything else on the device is unaffected.

**Add a device:**
```bash
ssh kunigi-vps 'sudo kunigi-wireguard add phone && sudo cat /etc/wireguard/clients/phone.conf'
```

**Remove a device:** delete its `[Peer]` block from `/etc/wireguard/wg0.conf`, then run `sudo systemctl restart wg-quick@wg0`.

## Backups

The nightly `vps-backup.sh` (#85):
- databases `KunigiArchiveV2` (the site) and `KunigiArchive` (old site, until removed): `BACKUP DATABASE ... WITH CHECKSUM`, then `RESTORE VERIFYONLY`; aborts if a `.bak` is under 1 MB
- folders `kunigi_web_media` (uploads), `kunigi_web_keys` (sign-in keys) and `kunigi_media` (old site, until removed)
- the env files from `/opt/kunigi`
- a missing database or folder is logged as `WARNING` and skipped (so removing the old data doesn't break it); any other error aborts with `BACKUP FAILED` and the SQL error in the log
- keeps the newest 3 archives; old ones are deleted only after a successful run

The lists are at the top of the script. After changing it, install it again with `sudo bash ops/vps/setup.sh` (or `sudo install -o root -g root -m 700 ops/vps/backup/vps-backup.sh /opt/kunigi-backups/`) and run it once by hand: `sudo /opt/kunigi-backups/vps-backup.sh`.

Archive layout (the `_v2` names are kept so older archives read the same way):

```
kunigi_backup_<ts>.tar.gz
└── backup_<ts>/
    ├── databases/
    │   ├── KunigiArchiveV2.bak   (the site)
    │   └── KunigiArchive.bak     (old site, until removed)
    ├── media_v2/...              (kunigi_web_media)
    ├── keys_v2/...               (kunigi_web_keys)
    ├── media/...                 (kunigi_media, old site)
    └── config/*.env
```

Off-site copies are made by the manual **Off-site backup** GitHub Action. It keeps one encrypted artifact. Decrypt it with `gpg --decrypt <file>.gpg > <file>`, using the `BACKUP_PASSPHRASE` secret.

### Restore (outline, not yet rehearsed — see #38)

1. Stop the site: `cd ~iperp/kunigi-archive && sudo docker compose stop web`
2. Extract the archive: `tar -xzf kunigi_backup_<ts>.tar.gz`
3. Database:
   `docker cp backup_<ts>/databases/KunigiArchiveV2.bak kunigi_db:/var/opt/mssql/data/restore.bak`, then
   `RESTORE DATABASE [KunigiArchiveV2] FROM DISK = N'/var/opt/mssql/data/restore.bak' WITH REPLACE`
   (as `kunigi_admin`), then `sudo bash ~iperp/kunigi-archive/ops/vps/init-secrets.sh` to restore the logins' rights.
4. Files: `rsync -a --delete backup_<ts>/media_v2/ /home/iperp/kunigi_web_media/` and
   `rsync -a --delete backup_<ts>/keys_v2/ /home/iperp/kunigi_web_keys/`, then `chown -R 1654:1654` both (the container user).
5. Start the site: `sudo docker compose up -d web`

## Access

- Owner: `iperp`
- Maintenance user `claude`: key-only SSH, passwordless sudo via `/etc/sudoers.d/90-claude`, docker group. Added 2026-11-14.
  Remove with `sudo deluser --remove-home claude && sudo rm /etc/sudoers.d/90-claude`.
- `antigravity`: sudo user of unknown purpose (#19).

Local SSH alias (`~/.ssh/config`):

```
Host kunigi-vps
    HostName tokinigi.gr
    User claude
    IdentityFile ~/.ssh/kunigi_claude
    IdentitiesOnly yes
```

## Change log

### 2026-11-19 — tokinigi.gr switched to the new app (#89)

- Both sites stopped; verified backup (both databases, both media folders, keys, env files) on the server and downloaded off it.
- Final import of the old data into `KunigiArchiveV2` (`import-beta.sh --reset --apply`): every row and file matched the old database.
- `/opt/kunigi/web.env` created from `beta.env` (links to `https://tokinigi.gr`); folders `kunigi_beta_{media,keys,logs}` renamed to `kunigi_web_*`; the old image tagged `kunigi-archive-web:old-mvc`.
- `kunigi_backup` re-granted on `KunigiArchiveV2` (the import had recreated the database); `vps-backup.sh` fixed to log `BACKUP FAILED` with the SQL error (it failed silently before) and pointed at the new folders.
- The repo's old MVC app and the beta stack were removed; `kunigi_web` now runs the new app. The old site's data, image and `app.env` are kept for a rollback.

### 2026-11-14 — disk cleanup (26 GB → 6.8 GB used)

- `docker builder prune -a -f`: 16.1 GB of build cache from building on the server at every deploy. Added the weekly prune cron.
- Journal capped at 200 MB (was 2 GB).
- Rewrote `vps-backup.sh` (error handling, DB verification, keep 3 instead of 8 days). The previous version is kept as `/opt/kunigi-backups/vps-backup.sh.old-2026-11-14`.
- SQL Server: `KunigiArchive` switched FULL → SIMPLE recovery and the log shrunk once (`DBCC SHRINKFILE (KunigiArchive_log, 16)`): 200 MB → 24 MB. `system_health` traces capped at 4 × 20 MB (were 845 MB). The DB volume went from 1.2 GB to 148 MB.

### 2026-11-14 — secrets moved out of the repo

- Ran `init-secrets.sh`: created `/opt/kunigi/{db,app,backup,admin}.env` and the logins `kunigi_app`, `kunigi_backup`, `kunigi_admin`. The `sa` password is unchanged for now (still the old, public one).
- The backup script now uses `kunigi_backup`, includes `config/*.env` in the archive, and makes archives `root:iperp 640`.
- WireGuard VPN set up (devices `desktop`, `laptop`). SQL Server published on 1433 and firewalled to the VPN only. `kunigi_db` was recreated once to publish the port (same volume, data verified). Backups of the edited files: `/etc/ufw/after.rules.bak-2026-11-14`, `/home/iperp/kunigi-archive/docker-compose.yml.bak-2026-11-14`.
- The app keeps using `sa` until the compose change (`env_file`) is deployed. After that deploy: `sudo ~iperp/kunigi-archive/ops/vps/init-secrets.sh --rotate-sa`, then rotate the Mailtrap token and update `EmailSettings__Password` in `app.env`.
