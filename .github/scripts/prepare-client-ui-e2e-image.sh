#!/usr/bin/env bash
set -euo pipefail

readonly source_dist="src/MARS.ClientUi/dist"
readonly build_context=".ci/client-ui"

if [[ ! -s "${source_dist}/index.html" ]]; then
    echo "::error::Production dist is missing. Run the frontend build before preparing the E2E image." >&2
    exit 1
fi

if [[ ! -f "infrastructure/nginx/client-ui.conf" ]]; then
    echo "::error::Nginx client configuration is missing." >&2
    exit 1
fi

# Recreate the context to prevent stale assets from an earlier local/CI run.
rm -rf "$build_context"
mkdir -p "${build_context}/dist"

cp -a "${source_dist}/." "${build_context}/dist/"
cp infrastructure/nginx/client-ui.conf "${build_context}/default.conf"

cat > "${build_context}/Dockerfile" <<'DOCKERFILE'
FROM nginx:alpine

COPY default.conf /etc/nginx/conf.d/default.conf
COPY dist/ /usr/share/nginx/html/

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=5s --retries=12 --start-period=10s \
    CMD wget --spider -q http://127.0.0.1/healthz || exit 1

STOPSIGNAL SIGQUIT
CMD ["nginx", "-g", "daemon off;"]
DOCKERFILE
