#!/bin/bash
# ==============================================================================
# Kunigi Archive - VPS host configuration. Idempotent: safe to re-run.
#
# Usage (on the VPS, from the deployed repo copy):
#   sudo ~iperp/kunigi-archive/ops/vps/setup.sh            # host config only
#   sudo ~iperp/kunigi-archive/ops/vps/setup.sh --sql      # + SQL Server tuning
#
# What it does:
#   1. Caps the systemd journal at 200 MB
#   2. Weekly Docker build-cache prune (Sunday 04:00)
#   3. Installs the nightly backup script and its root crontab entry (02:00)
#   4. Firewall: SQL Server (published on 1433) only reachable over WireGuard
#      (wg0); installs the kunigi-wireguard helper (see wireguard.sh)
#   5. (--sql) Applies sql/tune.sql inside the kunigi_db container
#
# It never touches the database volume, the media folder or the containers'
# lifecycle (no restarts, no compose up/down).
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APPLY_SQL=false
[ "${1:-}" = "--sql" ] && APPLY_SQL=true

if [ "$(id -u)" -ne 0 ]; then
    echo "Run as root (sudo)." >&2
    exit 1
fi

log() { echo "==> $*"; }

# 1. Journal size cap
log "journald: cap at 200M"
mkdir -p /etc/systemd/journald.conf.d
JOURNAL_CONF=/etc/systemd/journald.conf.d/size.conf
JOURNAL_WANT=$'[Journal]\nSystemMaxUse=200M'
if [ "$(cat "$JOURNAL_CONF" 2>/dev/null)" != "$JOURNAL_WANT" ]; then
    printf '%s\n' "$JOURNAL_WANT" > "$JOURNAL_CONF"
    systemctl restart systemd-journald
fi
journalctl --vacuum-size=200M >/dev/null

# 2. Weekly Docker build-cache prune
log "cron: weekly docker builder prune"
cat > /etc/cron.d/docker-builder-prune <<'EOF'
0 4 * * 0 root /usr/bin/docker builder prune -f --filter until=168h >/dev/null 2>&1
EOF
chmod 644 /etc/cron.d/docker-builder-prune

# 3. Backup script + nightly cron
log "backup: install /opt/kunigi-backups/vps-backup.sh"
mkdir -p /opt/kunigi-backups
install -o root -g root -m 700 "$SCRIPT_DIR/backup/vps-backup.sh" /opt/kunigi-backups/vps-backup.sh
sed -i 's/\r$//' /opt/kunigi-backups/vps-backup.sh

BACKUP_CRON='0 2 * * * /opt/kunigi-backups/vps-backup.sh >> /var/log/kunigi-backup.log 2>&1'
if ! crontab -l 2>/dev/null | grep -Fq "/opt/kunigi-backups/vps-backup.sh"; then
    log "backup: add root crontab entry"
    (crontab -l 2>/dev/null; echo "$BACKUP_CRON") | crontab -
fi

# 4. Firewall for the published SQL port + WireGuard helper
# Docker-published ports bypass ufw's INPUT rules, so filtering happens in the
# DOCKER-USER chain. ufw (re)creates it from after.rules on every start/reload.
log "firewall: SQL Server (1433) only via WireGuard"
if ! grep -q "BEGIN KUNIGI DOCKER-USER" /etc/ufw/after.rules; then
    cat >> /etc/ufw/after.rules <<'EOF'

# BEGIN KUNIGI DOCKER-USER (SQL Server only reachable over WireGuard)
*filter
:DOCKER-USER - [0:0]
-A DOCKER-USER -i wg0 -p tcp -m conntrack --ctorigdstport 1433 --ctdir ORIGINAL -j RETURN
-A DOCKER-USER -p tcp -m conntrack --ctorigdstport 1433 --ctdir ORIGINAL -j DROP
-A DOCKER-USER -j RETURN
COMMIT
# END KUNIGI DOCKER-USER
EOF
    ufw reload >/dev/null
fi
install -o root -g root -m 700 "$SCRIPT_DIR/wireguard.sh" /usr/local/sbin/kunigi-wireguard
sed -i 's/\r$//' /usr/local/sbin/kunigi-wireguard
command -v wg >/dev/null || DEBIAN_FRONTEND=noninteractive apt-get install -y -qq wireguard >/dev/null
kunigi-wireguard init >/dev/null

# 5. SQL Server tuning
if $APPLY_SQL; then
    log "sql: apply sql/tune.sql"
    SA_PASSWORD="${SA_PASSWORD:-$(grep -oP 'MSSQL_SA_PASSWORD=\K.*' "$SCRIPT_DIR/../../docker-compose.yml" | head -1)}"
    docker exec -i kunigi_db /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SA_PASSWORD" -C -b < "$SCRIPT_DIR/sql/tune.sql"
fi

log "done"
