#!/usr/bin/env bash
# Imports the live site's data (KunigiArchive + kunigi_media) into beta (KunigiArchiveV2 + kunigi_beta_media),
# with src/KunigiArchive.Import (#63). The live database and media folder are only read.
#
#   sudo bash ops/vps/import-beta.sh                 dry run: checks everything, writes nothing, prints the report
#   sudo bash ops/vps/import-beta.sh --apply         imports (replaces beta's teams, games and everything under them)
#   sudo bash ops/vps/import-beta.sh --reset --apply first drops beta's database and empties its media folder
#
# Runs the import in a throwaway .NET SDK container on the live stack's network (it reaches the "db" service),
# as beta's container user so the files it writes belong to beta. Uses the kunigi_admin login from admin.env:
# it's the one login that can read KunigiArchive and write KunigiArchiveV2. The code comes from the beta deploy
# folder, so run "Deploy beta" first to import with the latest master. The report is saved next to this
# script's deploy folder as import-report-<time>.txt.
set -euo pipefail

REPO=/home/iperp/kunigi-beta              # copied by the Deploy beta workflow
OLD_MEDIA=/home/iperp/kunigi_media        # live uploads, mounted read-only
NEW_MEDIA=/home/iperp/kunigi_beta_media   # beta uploads
SECRETS=/opt/kunigi
NETWORK=kunigi-archive_default
DB_CONTAINER=kunigi_db
BETA_CONTAINER=kunigi_beta
SOURCE_DB=KunigiArchive
TARGET_DB=KunigiArchiveV2
BETA_LOGIN=kunigi_beta
APP_UID=1654                              # APP_UID of the .NET images; beta runs as this user
SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
REPORT_DIR=/home/iperp

APPLY=false
RESET=false
for arg in "$@"; do
    case "$arg" in
        --apply) APPLY=true ;;
        --reset) RESET=true ;;
        *) echo "Unknown option: $arg" >&2; exit 2 ;;
    esac
done

log() { echo "[$(date '+%H:%M:%S')] $*"; }
env_get() { grep -E "^$2=" "$1" | head -1 | cut -d= -f2-; }

[ "$(id -u)" -eq 0 ] || { echo "Run with sudo." >&2; exit 1; }
[ -d "$REPO/src/KunigiArchive.Import" ] || { echo "$REPO has no src/KunigiArchive.Import: run Deploy beta first." >&2; exit 1; }
if $RESET && ! $APPLY; then echo "--reset only goes with --apply." >&2; exit 2; fi

SQL_USER=$(env_get "$SECRETS/admin.env" SQL_ADMIN_USER)
SQL_PASSWORD=$(env_get "$SECRETS/admin.env" SQL_ADMIN_PASSWORD)
[ -n "$SQL_USER" ] && [ -n "$SQL_PASSWORD" ] || { echo "SQL_ADMIN_USER / SQL_ADMIN_PASSWORD missing in $SECRETS/admin.env." >&2; exit 1; }

# The password goes through the environment, never the command line.
sql() {
    docker exec -i -e SQLCMDPASSWORD="$SQL_PASSWORD" "$DB_CONTAINER" \
        /opt/mssql-tools18/bin/sqlcmd -S localhost -U "$SQL_USER" -C -b -h -1 -I
}

if $RESET; then
    log "Stopping $BETA_CONTAINER"
    docker stop "$BETA_CONTAINER" > /dev/null

    log "Dropping and recreating $TARGET_DB (beta only; $SOURCE_DB is untouched)"
    sql <<SQL
SET NOCOUNT ON;
IF DB_ID(N'$TARGET_DB') IS NOT NULL
BEGIN
    ALTER DATABASE [$TARGET_DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$TARGET_DB];
END;
CREATE DATABASE [$TARGET_DB];
ALTER DATABASE [$TARGET_DB] SET RECOVERY SIMPLE;
SQL
    sql <<SQL
SET NOCOUNT ON;
USE [$TARGET_DB];
CREATE USER [$BETA_LOGIN] FOR LOGIN [$BETA_LOGIN];
ALTER ROLE db_owner ADD MEMBER [$BETA_LOGIN];
SQL

    log "Emptying $NEW_MEDIA"
    find "$NEW_MEDIA" -mindepth 1 -delete

    # Beta creates its schema and its first admin on startup; the import then signs that admin over to the
    # live account with the same email (same password as on the live site).
    log "Starting $BETA_CONTAINER (it migrates the new database)"
    docker start "$BETA_CONTAINER" > /dev/null
    for _ in $(seq 1 60); do
        if docker logs --since 2m "$BETA_CONTAINER" 2>&1 | grep -q "Now listening\|Application started"; then break; fi
        sleep 2
    done
fi

WORK=$(mktemp -d /tmp/kunigi-import.XXXXXX)
ENV_FILE=$(mktemp /tmp/kunigi-import-env.XXXXXX)
trap 'rm -rf "$WORK" "$ENV_FILE"' EXIT
chmod 600 "$ENV_FILE"

log "Copying the code from $REPO"
cp -r "$REPO/src" "$REPO/Directory.Build.props" "$REPO/Directory.Packages.props" "$REPO/global.json" "$WORK/"
mkdir -p "$WORK/home"
chown -R "$APP_UID:$APP_UID" "$WORK"

CONN="Server=db;User Id=$SQL_USER;Password=$SQL_PASSWORD;TrustServerCertificate=True;Encrypt=True"
cat > "$ENV_FILE" <<EOF
ConnectionStrings__Source=$CONN;Database=$SOURCE_DB;ApplicationIntent=ReadOnly
ConnectionStrings__KunigiArchive=$CONN;Database=$TARGET_DB
SourceMedia__RootPath=/old-media
MediaStorage__RootPath=/new-media
EOF

ARGS=()
$APPLY && ARGS+=(--apply)

log "Running the import ($($APPLY && echo apply || echo dry run))"
set +e
docker run --rm \
    --network "$NETWORK" \
    --user "$APP_UID:$APP_UID" \
    --env-file "$ENV_FILE" \
    -e HOME=/work/home \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    -e DOTNET_NOLOGO=1 \
    -v "$WORK:/work" \
    -v "$OLD_MEDIA:/old-media:ro" \
    -v "$NEW_MEDIA:/new-media" \
    -w /work \
    "$SDK_IMAGE" \
    dotnet run --project src/KunigiArchive.Import -c Release -- "${ARGS[@]}"
STATUS=$?
set -e

REPORT="$REPORT_DIR/import-report-$(date '+%Y-%m-%d_%H-%M-%S').txt"
if [ -f "$WORK/import-report.txt" ]; then
    cp "$WORK/import-report.txt" "$REPORT"
    chmod 640 "$REPORT"
    log "Report: $REPORT"
fi

exit $STATUS
