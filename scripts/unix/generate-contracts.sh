#!/usr/bin/env bash
#
# Генерация офлайн-снимков контрактов MARS.Alerts (OpenAPI REST + SignalR hubs)
# для фронтенда MARS.ClientUi.
#
# Примеры:
#   ./scripts/unix/generate-contracts.sh
#   ./scripts/unix/generate-contracts.sh --output /tmp/contracts
#
# Опции:
#   --output КАТАЛОГ   каталог для снимков (по умолчанию src/MARS.ClientUi/api)
#   --help             эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

output=""
while [ $# -gt 0 ]; do
    case "$1" in
        --output)
            output="$2"
            shift 2
            ;;
        --help)
            sed -n '1,40p' "$0"
            exit 0
            ;;
        *)
            echo "Неизвестный аргумент: $1" >&2
            exit 2
            ;;
    esac
done

if [ -z "$output" ]; then
    output="$MARS_ROOT/src/MARS.ClientUi/api"
fi

specs_project="$MARS_ROOT/src/MARS.Alerts/MARS.Alerts.Api.Specs/MARS.Alerts.Api.Specs.csproj"

echo "Генерация контрактов MARS.Alerts..."

dotnet run --project "$specs_project" --configuration Release -- --output "$output"

echo "Снимки контрактов обновлены в $output"