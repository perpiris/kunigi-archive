#!/bin/bash
# ==============================================================================
# Kunigi Archive - WireGuard VPN on the VPS (for SSMS / admin access).
#
#   sudo wireguard.sh init          create server keys + /etc/wireguard/wg0.conf,
#                                   open UDP 51820, start wg-quick@wg0
#   sudo wireguard.sh add <name>    add a device; writes its config to
#                                   /etc/wireguard/clients/<name>.conf
#   sudo wireguard.sh list          show devices and last handshakes
#
# VPN subnet 10.8.0.0/24: the VPS is 10.8.0.1, devices get .2, .3, ...
# Devices only route the VPN subnet through the tunnel (normal internet stays direct).
# On a device: import <name>.conf into the WireGuard app, activate, then use
# 10.8.0.1 (e.g. SSMS: 10.8.0.1,1433).
# ==============================================================================
set -euo pipefail

WG_DIR=/etc/wireguard
CONF="$WG_DIR/wg0.conf"
CLIENTS="$WG_DIR/clients"
SUBNET_PREFIX="10.8.0"
PORT=51820
ENDPOINT="tokinigi.gr:$PORT"

if [ "$(id -u)" -ne 0 ]; then
    echo "Run as root (sudo)." >&2
    exit 1
fi
umask 077

cmd="${1:-}"
case "$cmd" in
init)
    mkdir -p "$CLIENTS"
    if [ ! -f "$CONF" ]; then
        wg genkey > "$WG_DIR/server.key"
        wg pubkey < "$WG_DIR/server.key" > "$WG_DIR/server.pub"
        cat > "$CONF" <<EOF
[Interface]
Address = $SUBNET_PREFIX.1/24
ListenPort = $PORT
PrivateKey = $(cat "$WG_DIR/server.key")
EOF
        echo "Created $CONF"
    fi
    ufw allow "$PORT/udp" comment "WireGuard" >/dev/null
    systemctl enable --now wg-quick@wg0 >/dev/null 2>&1
    wg show wg0 | head -4
    ;;
add)
    name="${2:?usage: wireguard.sh add <name>}"
    [[ "$name" =~ ^[a-z0-9-]+$ ]] || { echo "name: lowercase letters, digits, dashes" >&2; exit 1; }
    if [ -f "$CLIENTS/$name.conf" ]; then
        echo "Device '$name' already exists: $CLIENTS/$name.conf"
        exit 0
    fi
    # next free address
    last=$(grep -oP "AllowedIPs = $SUBNET_PREFIX\.\K[0-9]+" "$CONF" | sort -n | tail -1 || true)
    ip="$SUBNET_PREFIX.$(( ${last:-1} + 1 ))"
    priv=$(wg genkey)
    pub=$(echo "$priv" | wg pubkey)
    cat >> "$CONF" <<EOF

[Peer]
# $name
PublicKey = $pub
AllowedIPs = $ip/32
EOF
    cat > "$CLIENTS/$name.conf" <<EOF
[Interface]
PrivateKey = $priv
Address = $ip/32

[Peer]
PublicKey = $(cat "$WG_DIR/server.pub")
Endpoint = $ENDPOINT
AllowedIPs = $SUBNET_PREFIX.0/24
PersistentKeepalive = 25
EOF
    wg syncconf wg0 <(wg-quick strip wg0)
    echo "Added '$name' as $ip -> $CLIENTS/$name.conf"
    ;;
list)
    wg show wg0
    ;;
*)
    sed -n '3,15p' "$0"
    exit 1
    ;;
esac
