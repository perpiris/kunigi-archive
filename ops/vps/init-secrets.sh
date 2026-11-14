#!/bin/bash
# ==============================================================================
# Kunigi Archive - secrets and SQL logins on the VPS. Idempotent: safe to re-run.
#
# Creates (only if missing) the env files that docker-compose.yml and the backup
# script read, and makes sure the SQL logins in them exist with those passwords:
#
#   /opt/kunigi/db.env       ACCEPT_EULA, MSSQL_SA_PASSWORD          (kunigi_db)
#   /opt/kunigi/app.env      ASPNETCORE_ENVIRONMENT, connection string
#                            (login kunigi_app, db_owner), SMTP password,
#                            AdminSeed__Email/Password               (kunigi_web)
#   /opt/kunigi/backup.env   BACKUP_SQL_USER/PASSWORD
#                            (login kunigi_backup, db_backupoperator) (vps-backup.sh)
#   /opt/kunigi/admin.env    SQL_ADMIN_USER/PASSWORD
#                            (login kunigi_admin, sysadmin)  for SSMS / manual work
#
# Usage (as root on the VPS):
#   init-secrets.sh                 create missing files, ensure logins
#   init-secrets.sh --rotate-sa     also give sa a new random password
#
# Optional inputs, only used when app.env is created for the first time:
#   SMTP_PASSWORD   SMTP password (default: taken from the previously deployed
#                   appsettings.json, if it still contains one)
#   ADMIN_EMAIL     email of the admin to seed on an empty database
#
# Passwords are generated randomly and never printed.
# ==============================================================================
set -euo pipefail

SECRETS_DIR=/opt/kunigi
DEPLOY_GROUP=iperp      # the deploy runs `docker compose` as this user; compose reads env_file itself
DB_CONTAINER=kunigi_db
DB_NAME=KunigiArchive
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
DEPLOY_DIR=/home/iperp/kunigi-archive
ROTATE_SA=false
[ "${1:-}" = "--rotate-sa" ] && ROTATE_SA=true

if [ "$(id -u)" -ne 0 ]; then
    echo "Run as root (sudo)." >&2
    exit 1
fi

umask 077
mkdir -p "$SECRETS_DIR"

log() { echo "==> $*"; }

# 32 random letters/digits with at least one upper, lower and digit (SQL policy safe)
genpw() {
    local p
    while :; do
        p=$(openssl rand -base64 64 | tr -dc 'A-Za-z0-9' | cut -c1-32)
        if [[ ${#p} -eq 32 && $p =~ [A-Z] && $p =~ [a-z] && $p =~ [0-9] ]]; then
            echo "$p"
            return
        fi
    done
}

env_get() { grep -m1 "^$2=" "$1" 2>/dev/null | cut -d= -f2- || true; }

sql_as_sa() {
    docker exec -i "$DB_CONTAINER" "$SQLCMD" -S localhost -U sa -P "$SA_PASSWORD" -C -b -h -1
}

# --- db.env -------------------------------------------------------------------
if [ ! -f "$SECRETS_DIR/db.env" ]; then
    log "Creating db.env (keeping the current sa password)"
    SA_PASSWORD=$(grep -oP 'MSSQL_SA_PASSWORD=\K.*' "$DEPLOY_DIR/docker-compose.yml" | head -1 || true)
    if [ -z "$SA_PASSWORD" ]; then
        echo "Cannot find the current sa password; create $SECRETS_DIR/db.env by hand." >&2
        exit 1
    fi
    printf 'ACCEPT_EULA=Y\nMSSQL_SA_PASSWORD=%s\n' "$SA_PASSWORD" > "$SECRETS_DIR/db.env"
fi
SA_PASSWORD=$(env_get "$SECRETS_DIR/db.env" MSSQL_SA_PASSWORD)
echo "SET NOCOUNT ON; SELECT 1;" | sql_as_sa > /dev/null
log "sa login works"

# --- backup.env ---------------------------------------------------------------
if [ ! -f "$SECRETS_DIR/backup.env" ]; then
    log "Creating backup.env"
    printf 'BACKUP_SQL_USER=kunigi_backup\nBACKUP_SQL_PASSWORD=%s\n' "$(genpw)" > "$SECRETS_DIR/backup.env"
fi

# --- admin.env ----------------------------------------------------------------
if [ ! -f "$SECRETS_DIR/admin.env" ]; then
    log "Creating admin.env"
    printf 'SQL_ADMIN_USER=kunigi_admin\nSQL_ADMIN_PASSWORD=%s\n' "$(genpw)" > "$SECRETS_DIR/admin.env"
fi

# --- app.env ------------------------------------------------------------------
if [ ! -f "$SECRETS_DIR/app.env" ]; then
    log "Creating app.env"
    SMTP="${SMTP_PASSWORD:-}"
    if [ -z "$SMTP" ] && [ -f "$DEPLOY_DIR/KunigiArchive.Web/appsettings.json" ]; then
        SMTP=$(grep -oP '"Password": "\K[^"]+' "$DEPLOY_DIR/KunigiArchive.Web/appsettings.json" | head -1 || true)
    fi
    [ -z "$SMTP" ] && echo "WARNING: no SMTP password found; set EmailSettings__Password in app.env" >&2
    cat > "$SECRETS_DIR/app.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=db;Database=$DB_NAME;User Id=kunigi_app;Password=$(genpw);TrustServerCertificate=True;
EmailSettings__Password=$SMTP
AdminSeed__Email=${ADMIN_EMAIL:-}
AdminSeed__Password=$(genpw)
EOF
fi

# Permissions: compose (run as $DEPLOY_GROUP by the deploy) must read db.env and
# app.env; backup.env and admin.env are root-only.
chown "root:$DEPLOY_GROUP" "$SECRETS_DIR"
chmod 750 "$SECRETS_DIR"
chown "root:$DEPLOY_GROUP" "$SECRETS_DIR/db.env" "$SECRETS_DIR/app.env"
chmod 640 "$SECRETS_DIR/db.env" "$SECRETS_DIR/app.env"
chown root:root "$SECRETS_DIR/backup.env" "$SECRETS_DIR/admin.env"
chmod 600 "$SECRETS_DIR/backup.env" "$SECRETS_DIR/admin.env"

# --- SQL logins -----------------------------------------------------------------
ensure_login() {
    local name="$1" password="$2" role="$3"
    log "SQL login $name ($role on $DB_NAME)"
    sql_as_sa > /dev/null <<EOF
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$name')
    CREATE LOGIN [$name] WITH PASSWORD = N'$password', CHECK_POLICY = ON, DEFAULT_DATABASE = [$DB_NAME];
ELSE
    ALTER LOGIN [$name] WITH PASSWORD = N'$password';
GO
USE [$DB_NAME];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$name')
    CREATE USER [$name] FOR LOGIN [$name];
ALTER ROLE [$role] ADD MEMBER [$name];
GO
EOF
}

APP_PASSWORD=$(env_get "$SECRETS_DIR/app.env" ConnectionStrings__DefaultConnection | grep -oP 'Password=\K[^;]+')
BACKUP_PASSWORD=$(env_get "$SECRETS_DIR/backup.env" BACKUP_SQL_PASSWORD)
ensure_login kunigi_app "$APP_PASSWORD" db_owner
ensure_login kunigi_backup "$BACKUP_PASSWORD" db_backupoperator
# RESTORE VERIFYONLY (used by vps-backup.sh) requires CREATE DATABASE permission
echo "USE [master]; GRANT CREATE ANY DATABASE TO [kunigi_backup];" | sql_as_sa > /dev/null

ADMIN_PASSWORD=$(env_get "$SECRETS_DIR/admin.env" SQL_ADMIN_PASSWORD)
log "SQL login kunigi_admin (sysadmin)"
sql_as_sa > /dev/null <<EOF
SET NOCOUNT ON;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'kunigi_admin')
    CREATE LOGIN [kunigi_admin] WITH PASSWORD = N'$ADMIN_PASSWORD', CHECK_POLICY = ON;
ELSE
    ALTER LOGIN [kunigi_admin] WITH PASSWORD = N'$ADMIN_PASSWORD';
ALTER SERVER ROLE [sysadmin] ADD MEMBER [kunigi_admin];
GO
EOF

# --- optional: rotate sa ----------------------------------------------------------
if $ROTATE_SA; then
    log "Rotating sa password"
    NEW_SA=$(genpw)
    printf 'ACCEPT_EULA=Y\nMSSQL_SA_PASSWORD=%s\n' "$NEW_SA" > "$SECRETS_DIR/db.env.new"
    echo "ALTER LOGIN [sa] WITH PASSWORD = N'$NEW_SA';" | sql_as_sa > /dev/null
    mv "$SECRETS_DIR/db.env.new" "$SECRETS_DIR/db.env"
    chown "root:$DEPLOY_GROUP" "$SECRETS_DIR/db.env"
    chmod 640 "$SECRETS_DIR/db.env"
    SA_PASSWORD="$NEW_SA"
    echo "SET NOCOUNT ON; SELECT 1;" | sql_as_sa > /dev/null
    log "sa password rotated and verified"
fi

log "done"
