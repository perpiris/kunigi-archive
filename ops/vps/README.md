# VPS operations

Everything about the production server that isn't the app itself.

- **Host:** `tokinigi.gr`, Debian 13, SSH on port 22
- **Site:** https://tokinigi.gr

## Files here

| File | What it is |
|---|---|
| `setup.sh` | Idempotent host configuration (journal cap, cache prune, backup script + cron, optional SQL tuning). Safe to re-run. |
| `wireguard.sh` | VPN helper, installed as `/usr/local/sbin/kunigi-wireguard` (`init`, `add <name>`, `list`). |
| `init-secrets.sh` | Creates the env files in `/opt/kunigi` (if missing) and the SQL logins they use. `--beta` also sets up the beta site (database, login, `beta.env`, folders). `--rotate-sa` gives `sa` a new password. Safe to re-run. |
| `backup/vps-backup.sh` | Nightly backup script. Installed to `/opt/kunigi-backups/vps-backup.sh` by `setup.sh`. |
| `sql/tune.sql` | SQL Server settings (SIMPLE recovery, smaller diagnostic traces). Applied by `setup.sh --sql`. |

To apply after changing any of them (the deploy workflow copies the repo to `~iperp/kunigi-archive`):

```bash
ssh kunigi-vps 'sudo ~iperp/kunigi-archive/ops/vps/setup.sh --sql'
```

## Layout on the server

| What | Where |
|---|---|
| Deployed repo / compose project | `/home/iperp/kunigi-archive` (compose project `kunigi-archive`) |
| Beta site | `/home/iperp/kunigi-beta`, compose project `kunigi-beta` (`docker-compose.beta.yml`), container `kunigi_beta`, https://beta.tokinigi.gr (sent with `X-Robots-Tag: noindex`). Secrets in `/opt/kunigi/beta.env`. Joins the `kunigi-archive_default` network and uses `kunigi_db`. |
| Beta data | DB `KunigiArchiveV2` (same SQL Server), uploads in `/home/iperp/kunigi_beta_media`, sign-in keys in `/home/iperp/kunigi_beta_keys` (both owned by uid 1654, the container user). **Not in the nightly backup**: beta holds test data until the cutover (#63). |
| Containers | `kunigi_web` (.NET app, :8080), `kunigi_db` (SQL Server 2022), `nginx-proxy`, `nginx-acme-companion` |
| **Uploaded media** | `/home/iperp/kunigi_media` → mounted in `kunigi_web` at `/app/media` |
| **Database** | Docker volume `kunigi-archive_db_data` (`/var/lib/docker/volumes/kunigi-archive_db_data/_data`), DB `KunigiArchive` |
| App logs | Inside `kunigi_web` at `/app/Logs` (lost on rebuild), plus `docker logs kunigi_web` |
| Backups | `/opt/kunigi-backups/kunigi_backup_<timestamp>.tar.gz` (newest 3 kept) |
| Backup log | `/var/log/kunigi-backup.log` |
| Backup cron | root crontab, `0 2 * * *` |
| Cache prune cron | `/etc/cron.d/docker-builder-prune` (Sundays 04:00) |
| Journal cap | `/etc/systemd/journald.conf.d/size.conf` (200 MB) |

**Never** run `docker compose down -v`, and never rename the compose project or volumes: that deletes or detaches the database.

## Secrets and SQL logins

Secrets exist **only on the VPS**, in `/opt/kunigi`. The dir is `root:iperp 750`. `db.env` and `app.env` are `root:iperp 640`, because the deploy runs `docker compose` as `iperp` and compose reads `env_file` itself. `backup.env` and `admin.env` are root-only (`600`). They are not in the repo or in GitHub. `docker-compose.yml` reads them via `env_file`, and they are included in every backup (`config/`).

| File | Used by | Contents |
|---|---|---|
| `db.env` | `kunigi_db` | `ACCEPT_EULA`, `MSSQL_SA_PASSWORD` |
| `app.env` | `kunigi_web` | `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection` (login `kunigi_app`), `EmailSettings__Password`, `AdminSeed__Email`, `AdminSeed__Password` |
| `beta.env` | `kunigi_beta` | `ConnectionStrings__KunigiArchive` (login `kunigi_beta`), `AdminSeed__Email` (same as live), `AdminSeed__Password` (beta's own), `EmailSettings__Password` (same as live), `EmailSettings__ApplicationUrl` |
| `backup.env` | `vps-backup.sh` | `BACKUP_SQL_USER`, `BACKUP_SQL_PASSWORD` (login `kunigi_backup`) |
| `admin.env` | you (SSMS) | `SQL_ADMIN_USER`, `SQL_ADMIN_PASSWORD` (login `kunigi_admin`) |

| SQL login | Rights | Purpose |
|---|---|---|
| `kunigi_app` | `db_owner` on `KunigiArchive` | the app (owner rights because migrations run at startup) |
| `kunigi_beta` | `db_owner` on `KunigiArchiveV2` only | the beta site (Web2 runs its migrations at startup) |
| `kunigi_backup` | `db_backupoperator` + `CREATE ANY DATABASE` (needed for `RESTORE VERIFYONLY`) | nightly backup |
| `kunigi_admin` | `sysadmin` | manual administration / SSMS |
| `sa` | `sysadmin` | nothing automated; rotate with `init-secrets.sh --rotate-sa` |

Any `appsettings.json` key can be overridden in `app.env` with `Section__Key=value`. After editing `app.env`, recreate the app: `cd ~iperp/kunigi-archive && sudo docker compose up -d web`.

To see a value: `ssh kunigi-vps 'sudo cat /opt/kunigi/admin.env'`. The beta admin's password: `ssh kunigi-vps 'sudo grep AdminSeed /opt/kunigi/beta.env'` (it only seeds the first admin; change it later in the app).

### Beta site setup

Once, before the first Web2 deploy (and safe to re-run):

```bash
ssh kunigi-vps 'sudo bash ~iperp/kunigi-beta/ops/vps/init-secrets.sh --beta'
```

Then run the **Deploy beta** workflow. On its first start Web2 creates its tables and seeds the roles and the admin from `beta.env`. After editing `beta.env`, recreate the container: `cd ~iperp/kunigi-beta && sudo docker compose -f docker-compose.beta.yml up -d`.

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

The nightly `vps-backup.sh` (#85) covers both sites:
- databases `KunigiArchive` (old app) and `KunigiArchiveV2` (Web2): `BACKUP DATABASE ... WITH CHECKSUM`, then `RESTORE VERIFYONLY`; aborts if a `.bak` is under 1 MB
- folders `kunigi_media` (old app), `kunigi_beta_media` (Web2 uploads) and `kunigi_beta_keys` (Web2 sign-in keys)
- the env files from `/opt/kunigi`
- a missing database or folder is logged as `WARNING` and skipped (so removing the old app doesn't break it); any other error aborts with `BACKUP FAILED`
- keeps the newest 3 archives; old ones are deleted only after a successful run

The lists are at the top of the script. After changing it, install it again with `sudo bash ops/vps/setup.sh` (or `sudo install -o root -g root -m 700 ops/vps/backup/vps-backup.sh /opt/kunigi-backups/`) and run it once by hand: `sudo /opt/kunigi-backups/vps-backup.sh`.

Archive layout:

```
kunigi_backup_<ts>.tar.gz
└── backup_<ts>/
    ├── databases/
    │   ├── KunigiArchive.bak
    │   └── KunigiArchiveV2.bak
    ├── media/...        (old app)
    ├── media_v2/...     (Web2)
    ├── keys_v2/...      (Web2)
    └── config/*.env
```

Off-site copies are made by the manual **Off-site backup** GitHub Action. It keeps one encrypted artifact. Decrypt it with `gpg --decrypt <file>.gpg > <file>`, using the `BACKUP_PASSPHRASE` secret.

### Restore Web2 (outline, not yet rehearsed — see #38)

1. Stop the site's container.
2. Extract the archive: `tar -xzf kunigi_backup_<ts>.tar.gz`
3. Database:
   `docker cp backup_<ts>/databases/KunigiArchiveV2.bak kunigi_db:/var/opt/mssql/data/restore.bak`, then
   `RESTORE DATABASE [KunigiArchiveV2] FROM DISK = N'/var/opt/mssql/data/restore.bak' WITH REPLACE`
4. Files: `rsync -a --delete backup_<ts>/media_v2/ /home/iperp/kunigi_beta_media/` and
   `rsync -a --delete backup_<ts>/keys_v2/ /home/iperp/kunigi_beta_keys/`, then `chown -R 1654:1654` both (the container user).
5. Start the container again.

The old app restores the same way from `KunigiArchive.bak` and `media/` into `/home/iperp/kunigi_media/`.

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
