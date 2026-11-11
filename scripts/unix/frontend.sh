#!/usr/bin/env bash
#
# Frontend Gateway: type checks, tests and build.
#
# UI MediaStorage реализован на ASP.NET Core Razor Pages и vanilla JavaScript
# внутри MARS.MediaStorage.Host; отдельной Node/npm-сборки для него нет.
# Единственный отдельно собираемый JS frontend — src/MARS.ClientUi (Yarn 4).
#
# Для клиента шлюза npm ci не подходит: он требует package-lock.json, которого
# нет, и не выполняет нужные postinstall-скрипты. Образ client-ui собирает его
# тем же инструментом: corepack enable && yarn install --immutable.
#
# SKIP_STORYBOOK_VITEST=1 обязателен: без него vitest поднимает сюжеты
# Storybook, а тот ставит Playwright и качает Chromium — минуты загрузки и
# лишний браузер без единой проверки.
#
# `yarn test` в манифесте — это vitest в watch-режиме: без `run` он не
# завершится и скрипт зависнет.
#
# Примеры:
#   ./scripts/unix/frontend.sh                       полный цикл Gateway
#   ./scripts/unix/frontend.sh --client gateway --action check
#
# Опции:
#   --client gateway|all           чей фронтенд (по умолчанию all)
#   --action install|typecheck|test|build|check|all
#   --help                          эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

client="all"
action="all"

while [ $# -gt 0 ]; do
    case "$1" in
        --client) client="$2"; shift 2 ;;
        --action) action="$2"; shift 2 ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

case "$client" in
    gateway|all) ;;
    *) fail "Поддерживается только --client gateway или --client all." ;;
esac

gateway_path="src/MARS.ClientUi"

wants() {
    case "$action" in
        install) [ "$1" = "install" ] ;;
        typecheck) [ "$1" = "typecheck" ] ;;
        test) [ "$1" = "test" ] ;;
        build) [ "$1" = "build" ] ;;
        check) [ "$1" = "typecheck" ] || [ "$1" = "test" ] ;;
        all) [ "$1" != "skip" ] ;;
        *) fail "Неизвестное действие: $action" ;;
    esac
}

run_in_client() {
    local directory="$1"
    shift

    (
        cd "$directory"
        run "$@"
    )
}

step "Фронтенд: $client, действие $action"

failures=""

if [ "$client" = "gateway" ] || [ "$client" = "all" ]; then
    info "$gateway_path — Yarn 4, клиент оверлеев, админки и сайта"

    if wants install; then
        need_cmd corepack "Идёт в составе Node 22+. Без него Yarn 4 не включится."
        run_or_fail corepack "corepack enable" enable
        run_in_client "$gateway_path" yarn install --immutable
    fi

    if wants typecheck; then
        run_in_client "$gateway_path" npx tsc -b --noEmit
    fi

    if wants test; then
        # SKIP_STORYBOOK_VITEST ставится только на время команды: переменная
        # экспортируется в окружение текущего вызова, а не остаётся в сессии.
        if ! SKIP_STORYBOOK_VITEST=1 run_in_client "$gateway_path" yarn test run; then
            failures="$failures gateway"
        fi
    fi

    if wants build; then
        run_in_client "$gateway_path" yarn build
    fi
fi


step "Итог"

if [ -n "$failures" ]; then
    printf '    x Красные фронтенды:%s\n' "$failures" >&2
    exit 1
fi

ok "Фронтенды зелёные."
