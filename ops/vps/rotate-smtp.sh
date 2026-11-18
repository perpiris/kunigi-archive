#!/bin/bash
# ==============================================================================
# Kunigi Archive - replace the SMTP password (the Mailtrap API token, #37).
#
# Asks for the new token without echoing it, writes it as EmailSettings__Password into every env file that has
# one (/opt/kunigi/app.env, /opt/kunigi/beta.env), keeping each file's owner and mode, then recreates the
# containers that read them so they pick it up. Nothing is printed or logged.
#
# Usage (as root on the VPS, in an interactive shell):
#   sudo bash rotate-smtp.sh
# Afterwards: send a test email (e.g. "forgot password" for your own account) and delete the old token in Mailtrap.
# ==============================================================================
set -euo pipefail

SECRETS_DIR=/opt/kunigi
LIVE_DIR=/home/iperp/kunigi-archive
BETA_DIR=/home/iperp/kunigi-beta

[ "$(id -u)" -eq 0 ] || { echo "Run as root: sudo bash $0" >&2; exit 1; }

read -rsp "New Mailtrap API token: " TOKEN; echo
read -rsp "Again: " AGAIN; echo
[ -n "$TOKEN" ] || { echo "Empty token, nothing changed." >&2; exit 1; }
[ "$TOKEN" = "$AGAIN" ] || { echo "The two entries differ, nothing changed." >&2; exit 1; }

for file in "$SECRETS_DIR"/app.env "$SECRETS_DIR"/beta.env; do
    [ -f "$file" ] || continue
    grep -q '^EmailSettings__Password=' "$file" || continue
    # Rewritten in place (cat >), so the file keeps its owner and mode.
    { grep -v '^EmailSettings__Password=' "$file"; printf 'EmailSettings__Password=%s\n' "$TOKEN"; } > "$file.new"
    cat "$file.new" > "$file"
    rm -f "$file.new"
    echo "Updated $file"
done
unset TOKEN AGAIN

# env_file changes need a recreate (not a restart). --no-build: same image, new environment.
if [ -f "$LIVE_DIR/docker-compose.yml" ]; then
    echo "Recreating the live site..."
    (cd "$LIVE_DIR" && docker compose up -d --no-build web)
fi
if [ -f "$BETA_DIR/docker-compose.beta.yml" ]; then
    echo "Recreating beta..."
    (cd "$BETA_DIR" && docker compose -f docker-compose.beta.yml up -d --no-build beta)
fi

echo "Done. Send a test email, then delete the old token in Mailtrap."
