#!/bin/bash
# ==============================================================================
# Kunigi Archive - nightly backup (root crontab: 0 2 * * *)
# Backs up the SQL Server database and the uploaded media into one archive:
#   kunigi_backup_<timestamp>.tar.gz
#     backup_<timestamp>/database.bak
#     backup_<timestamp>/media/...
#     backup_<timestamp>/config/*.env     (secrets from /opt/kunigi)
# Old archives are deleted only after a new one has been created and verified.
# ==============================================================================
set -euo pipefail

BACKUP_DIR="/opt/kunigi-backups"
KEEP=3                                  # number of archives to keep locally
READ_GROUP="iperp"                      # group allowed to read archives (off-site backup action downloads as this user)
SECRETS_DIR="/opt/kunigi"
DB_CONTAINER="kunigi_db"
WEB_CONTAINER="kunigi_web"
DB_NAME="KunigiArchive"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

# SQL login with db_backupoperator rights (created by ops/vps/init-secrets.sh)
# shellcheck source=/dev/null
source "$SECRETS_DIR/backup.env"
DB_USER="${BACKUP_SQL_USER:?BACKUP_SQL_USER missing in $SECRETS_DIR/backup.env}"
DB_PASS="${BACKUP_SQL_PASSWORD:?BACKUP_SQL_PASSWORD missing in $SECRETS_DIR/backup.env}"

TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
WORK_DIR="$BACKUP_DIR/backup_$TIMESTAMP"
ARCHIVE="$BACKUP_DIR/kunigi_backup_$TIMESTAMP.tar.gz"
TMP_ARCHIVE="$ARCHIVE.partial"
CONTAINER_BAK="/var/opt/mssql/data/db_backup_$TIMESTAMP.bak"

log() { echo "[$(date +"%F %T")] $*"; }

cleanup() {
    rm -rf "$WORK_DIR" "$TMP_ARCHIVE"
    docker exec "$DB_CONTAINER" rm -f "$CONTAINER_BAK" 2>/dev/null || true
}
trap cleanup EXIT
trap 'log "BACKUP FAILED (line $LINENO)"; exit 1' ERR

sql() {
    docker exec "$DB_CONTAINER" "$SQLCMD" -S localhost -U "$DB_USER" -P "$DB_PASS" -C -b -Q "$1"
}

log "Starting backup $TIMESTAMP"
mkdir -p "$WORK_DIR"

# 1. Database: full backup with checksums, then verify it is restorable
log "Backing up database..."
sql "BACKUP DATABASE [$DB_NAME] TO DISK = N'$CONTAINER_BAK' WITH FORMAT, INIT, CHECKSUM"
sql "RESTORE VERIFYONLY FROM DISK = N'$CONTAINER_BAK' WITH CHECKSUM"
docker cp "$DB_CONTAINER:$CONTAINER_BAK" "$WORK_DIR/database.bak"

DB_SIZE=$(stat -c %s "$WORK_DIR/database.bak")
if [ "$DB_SIZE" -lt 1048576 ]; then
    log "database.bak is only $DB_SIZE bytes, aborting"
    exit 1
fi
log "database.bak: $DB_SIZE bytes (verified)"

# 2. Media: read straight from the host folder mounted at /app/media
MEDIA_SRC=$(docker inspect -f '{{range .Mounts}}{{if eq .Destination "/app/media"}}{{.Source}}{{end}}{{end}}' "$WEB_CONTAINER")
if [ -z "$MEDIA_SRC" ] || [ ! -d "$MEDIA_SRC" ]; then
    log "Could not find the media folder mounted at /app/media, aborting"
    exit 1
fi
MEDIA_FILES=$(find "$MEDIA_SRC" -type f | wc -l)
log "Media: $MEDIA_SRC ($MEDIA_FILES files)"

# 3. Secrets: needed to bring a restored server back up
mkdir -p "$WORK_DIR/config"
cp -a "$SECRETS_DIR"/*.env "$WORK_DIR/config/"
log "Config: $(ls "$WORK_DIR/config" | tr '\n' ' ')"

# 4. Archive (written to a temp name, renamed only when complete)
log "Compressing..."
tar -czf "$TMP_ARCHIVE" \
    -C "$BACKUP_DIR" "backup_$TIMESTAMP" \
    -C "$(dirname "$MEDIA_SRC")" --transform "s,^$(basename "$MEDIA_SRC"),backup_$TIMESTAMP/media," "$(basename "$MEDIA_SRC")"
tar -tzf "$TMP_ARCHIVE" > /dev/null
chown "root:$READ_GROUP" "$TMP_ARCHIVE"
chmod 640 "$TMP_ARCHIVE"                # archives contain secrets (config/*.env)
mv "$TMP_ARCHIVE" "$ARCHIVE"
log "Archive: $ARCHIVE ($(du -h "$ARCHIVE" | cut -f1))"

# 5. Retention: keep the newest $KEEP archives
ls -1t "$BACKUP_DIR"/kunigi_backup_*.tar.gz | tail -n +$((KEEP + 1)) | while read -r old; do
    log "Deleting old backup $old"
    rm -f "$old"
done

log "Backup completed successfully"
