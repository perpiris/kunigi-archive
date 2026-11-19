#!/bin/bash
# ==============================================================================
# Kunigi Archive - nightly backup (root crontab: 0 2 * * *)
# Backs up the SQL Server databases, the uploaded media and the sign-in keys into one archive:
#   kunigi_backup_<timestamp>.tar.gz
#     backup_<timestamp>/databases/<name>.bak
#     backup_<timestamp>/<folder>/...       (see FOLDERS)
#     backup_<timestamp>/config/*.env       (secrets from /opt/kunigi)
# Old archives are deleted only after a new one has been created and verified.
#
# Covers the site: database KunigiArchiveV2, uploads (kunigi_web_media) and sign-in keys (kunigi_web_keys).
# A database or folder that doesn't exist is logged as a WARNING and skipped; anything else that fails aborts
# the run with BACKUP FAILED. The "_v2" names inside the archive are kept so older archives and the off-site
# check read the same way. (The old MVC site's database and media were removed on 2026-11-19, #89.)
# ==============================================================================
# -E: the ERR trap below also fires inside functions (sql), so a failure is always logged as BACKUP FAILED.
set -Eeuo pipefail

BACKUP_DIR="/opt/kunigi-backups"
KEEP=3                                  # number of archives to keep locally
READ_GROUP="iperp"                      # group allowed to read archives (off-site backup action downloads as this user)
SECRETS_DIR="/opt/kunigi"
DB_CONTAINER="kunigi_db"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

DATABASES=(KunigiArchiveV2)

# host folder -> name inside the archive
FOLDERS=(
    "/home/iperp/kunigi_web_media:media_v2"         # the site's uploads
    "/home/iperp/kunigi_web_keys:keys_v2"           # the site's Data Protection keys (sign-in cookies, forms)
)

# SQL login with db_backupoperator rights on each database (created by ops/vps/init-secrets.sh)
# shellcheck source=/dev/null
source "$SECRETS_DIR/backup.env"
DB_USER="${BACKUP_SQL_USER:?BACKUP_SQL_USER missing in $SECRETS_DIR/backup.env}"
DB_PASS="${BACKUP_SQL_PASSWORD:?BACKUP_SQL_PASSWORD missing in $SECRETS_DIR/backup.env}"

TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
WORK_DIR="$BACKUP_DIR/backup_$TIMESTAMP"
ARCHIVE="$BACKUP_DIR/kunigi_backup_$TIMESTAMP.tar.gz"
TMP_ARCHIVE="$ARCHIVE.partial"
WARNINGS=0

LOG_LINES=()
log() { local line; line="[$(date +"%F %T")] $*"; echo "$line"; LOG_LINES+=("$line"); }
warn() { log "WARNING: $*"; WARNINGS=$((WARNINGS + 1)); }

# Emails the owner when a run fails or warns (#38), through the site's own SMTP account (Mailtrap): the token and
# the owner's address are read from web.env (AdminSeed__Email), or BACKUP_ALERT_EMAIL in backup.env. A failure to
# send is only logged: the alert must never break the backup itself.
SMTP_URL="smtp://live.smtp.mailtrap.io:587"
SMTP_FROM="hello@tokinigi.gr"
alert() {
    local subject="$1" token to
    token=$(grep -m1 '^EmailSettings__Password=' "$SECRETS_DIR/web.env" 2>/dev/null | cut -d= -f2- || true)
    to="${BACKUP_ALERT_EMAIL:-$(grep -m1 '^AdminSeed__Email=' "$SECRETS_DIR/web.env" 2>/dev/null | cut -d= -f2- || true)}"
    if [ -z "$token" ] || [ -z "$to" ]; then
        echo "[$(date +"%F %T")] Alert not sent: no SMTP token or recipient in $SECRETS_DIR"
        return 0
    fi
    {
        printf 'From: Kunigi backup <%s>\r\nTo: %s\r\nSubject: %s\r\nContent-Type: text/plain; charset=utf-8\r\n\r\n' \
            "$SMTP_FROM" "$to" "$subject"
        printf 'The nightly backup on %s needs a look. Its log for this run:\r\n\r\n' "$(hostname)"
        printf '%s\r\n' "${LOG_LINES[@]}"
        printf '\r\nFull log: /var/log/kunigi-backup.log\r\n'
    } | curl -sS --max-time 30 --ssl-reqd --url "$SMTP_URL" --user "api:$token" \
            --mail-from "$SMTP_FROM" --mail-rcpt "$to" --upload-file - \
        && echo "[$(date +"%F %T")] Alert sent" \
        || echo "[$(date +"%F %T")] Alert could not be sent"
}

cleanup() {
    rm -rf "$WORK_DIR" "$TMP_ARCHIVE"
    docker exec "$DB_CONTAINER" sh -c "rm -f /var/opt/mssql/data/db_backup_${TIMESTAMP}_*.bak" 2>/dev/null || true
}
trap cleanup EXIT
trap 'log "BACKUP FAILED (line $LINENO)"; alert "Kunigi backup FAILED on $(hostname)"; exit 1' ERR

# vps-backup.sh --test-alert: send a test alert email and stop (checks the SMTP token and the recipient).
if [ "${1:-}" = "--test-alert" ]; then
    log "Test alert, no backup made"
    alert "Kunigi backup alert test on $(hostname)"
    exit 0
fi

sql() {
    docker exec "$DB_CONTAINER" "$SQLCMD" -S localhost -U "$DB_USER" -P "$DB_PASS" -C -b -h -1 -W -Q "SET NOCOUNT ON; $1"
}

log "Starting backup $TIMESTAMP"
mkdir -p "$WORK_DIR/databases"

# 1. Databases: full backup with checksums, then verify each is restorable
for db in "${DATABASES[@]}"; do
    # A plain assignment, so a failed query (can't connect, wrong password) aborts the run as BACKUP FAILED
    # instead of being mistaken for a missing database. (On 2026-11-19 the backup login's default database was
    # dropped and the site's database was silently skipped.)
    exists=$(sql "SELECT COUNT(*) FROM sys.databases WHERE name = N'$db'")
    if [ "$exists" != "1" ]; then
        warn "database $db not found, skipped"
        continue
    fi

    container_bak="/var/opt/mssql/data/db_backup_${TIMESTAMP}_$db.bak"
    log "Backing up database $db..."
    sql "BACKUP DATABASE [$db] TO DISK = N'$container_bak' WITH FORMAT, INIT, CHECKSUM"
    sql "RESTORE VERIFYONLY FROM DISK = N'$container_bak' WITH CHECKSUM"
    docker cp "$DB_CONTAINER:$container_bak" "$WORK_DIR/databases/$db.bak"
    docker exec "$DB_CONTAINER" rm -f "$container_bak"

    size=$(stat -c %s "$WORK_DIR/databases/$db.bak")
    if [ "$size" -lt 1048576 ]; then
        log "$db.bak is only $size bytes, aborting"
        exit 1
    fi
    log "$db.bak: $size bytes (verified)"
done

# 2. Folders: linked into the work dir and archived through the links (tar -h)
for entry in "${FOLDERS[@]}"; do
    src="${entry%%:*}"
    name="${entry##*:}"
    if [ ! -d "$src" ]; then
        warn "folder $src not found, skipped"
        continue
    fi
    ln -s "$src" "$WORK_DIR/$name"
    log "Folder $name: $src ($(find "$src" -type f | wc -l) files)"
done

# 3. Secrets: needed to bring a restored server back up
mkdir -p "$WORK_DIR/config"
cp -a "$SECRETS_DIR"/*.env "$WORK_DIR/config/"
log "Config: $(ls "$WORK_DIR/config" | tr '\n' ' ')"

# 4. Archive (written to a temp name, renamed only when complete)
log "Compressing..."
tar -czhf "$TMP_ARCHIVE" -C "$BACKUP_DIR" "backup_$TIMESTAMP"
tar -tzf "$TMP_ARCHIVE" > /dev/null
chown "root:$READ_GROUP" "$TMP_ARCHIVE"
chmod 640 "$TMP_ARCHIVE"                # archives contain secrets (config/*.env) and the sign-in keys
mv "$TMP_ARCHIVE" "$ARCHIVE"
log "Archive: $ARCHIVE ($(du -h "$ARCHIVE" | cut -f1))"

# 5. Retention: keep the newest $KEEP archives
ls -1t "$BACKUP_DIR"/kunigi_backup_*.tar.gz | tail -n +$((KEEP + 1)) | while read -r old; do
    log "Deleting old backup $old"
    rm -f "$old"
done

if [ "$WARNINGS" -gt 0 ]; then
    log "Backup completed with $WARNINGS warning(s)"
    alert "Kunigi backup finished with $WARNINGS warning(s) on $(hostname)"
else
    log "Backup completed successfully"
fi
