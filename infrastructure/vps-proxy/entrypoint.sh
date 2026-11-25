#!/bin/sh
set -e
VPS_MIXED_PORT="${VPS_MIXED_PORT:-7890}"
VPS_PROXY_GROUP="${VPS_PROXY_GROUP:-VPS}"
export VPS_MIXED_PORT VPS_PROXY_GROUP

if [ -z "$VPS_SUBSCRIPTION_URL" ]; then
  cat > /tmp/config.yaml <<EOF
mixed-port: ${VPS_MIXED_PORT}
allow-lan: true
bind-address: "*"
mode: rule
log-level: info
rules:
  - MATCH,DIRECT
EOF
else
  export VPS_SUBSCRIPTION_URL
  envsubst < /opt/vps-proxy/config.template.yaml > /tmp/config.yaml
fi
exec /usr/bin/mihomo -f /tmp/config.yaml