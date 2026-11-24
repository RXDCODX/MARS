#!/usr/bin/env bash
#
# Проверка актуальности снимков контрактов MARS.Alerts в MARS.ClientUi.
#
# Примеры:
#   ./scripts/unix/check-contracts.sh

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

committed_dir="$MARS_ROOT/src/MARS.ClientUi/api"
tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

"$MARS_SCRIPT_DIR/generate-contracts.sh" --output "$tmp_dir"

failed=0
for file in swagger_alerts_v1.json swagger_alerts_hubs.json; do
    committed="$committed_dir/$file"
    generated="$tmp_dir/$file"
    if [ ! -f "$committed" ]; then
        echo "Коммиченный снимок отсутствует: $committed" >&2
        failed=1
        continue
    fi
    if [ ! -f "$generated" ]; then
        echo "Сгенерированный файл отсутствует: $generated" >&2
        failed=1
        continue
    fi
    if ! cmp -s "$committed" "$generated"; then
        echo "Расхождение контракта: $file" >&2
        failed=1
    fi
done

if [ "$failed" -ne 0 ]; then
    echo "Контракты Alerts устарели. Запусти ./scripts/unix/generate-contracts.sh и закоммить результат." >&2
    exit 1
fi

echo "Контракты Alerts актуальны"