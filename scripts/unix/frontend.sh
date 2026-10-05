#!/usr/bin/env bash
#
# Оба фронтенда репозитория: типы, тесты и сборка.
#
# Фронтендов в репозитории два, и инструменты у них разные. Путать их нельзя:
# наборы зависимостей, проверок и ловушек не совпадают.
#
#   src/MARS.Gateway/ClientApp      оверлеи, админка, сайт   Yarn 4, yarn.lock
#   src/MARS.MediaStorage/ClientApp страница хранилища        npm, package-lock.json
#
# Для клиента шлюза `npm ci` нерабочий: он требует package-lock.json, которого
# в репозитории нет, и не выполняет postinstall-скрипты, которые разрешает
# .yarnrc.yml. Второй lock-файл рядом с первым — это два источника правды, и
# собирать надо тем же инструментом, которым собирается образ. Образ client-ui
# собирает ровно так же: corepack enable && yarn install --immutable.
#
# SKIP_STORYBOOK_VITEST=1 обязателен: без него vitest поднимает сюжеты
# Storybook, а тот ставит Playwright и качает Chromium — минуты загрузки и
# лишний браузер без единой проверки.
#
# `yarn test` в манифесте — это vitest в watch-режиме: без `run` он не
# завершится и скрипт зависнет.
#
# Сборка ui-dist хранилища делается только по `--action build`: она происходит
# внутри образа, а результат в ui-dist/ в .gitignore.
#
# Примеры:
#   ./scripts/unix/frontend.sh                       полный цикл по обоим
#   ./scripts/unix/frontend.sh --client gateway --action check
#   ./scripts/unix/frontend.sh --client storage --action build
#
# Опции:
#   --client gateway|storage|all   чей фронтенд (по умолчанию all)
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

gateway_path="src/MARS.Gateway/ClientApp"
storage_path="src/MARS.MediaStorage/ClientApp"

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

if [ "$client" = "storage" ] || [ "$client" = "all" ]; then
    info "$storage_path — npm, страница хранилища"

    need_cmd npm "Node.js 22+ вместе с npm."

    if wants install; then
        run_in_client "$storage_path" npm ci
    fi

    if wants typecheck; then
        run_in_client "$storage_path" npm run typecheck
    fi

    if wants test; then
        note "У страницы хранилища нет тестов: в package.json нет test-скрипта. Проверяются типы и сборка."
        run_in_client "$storage_path" npm run typecheck
    fi

    if wants build; then
        # ui-dist собирается внутри образа; локальная сборка — только чтобы
        # убедиться, что конфигурация vite и препроцессоров в порядке.
        run_in_client "$storage_path" npm run build
    fi
fi

step "Итог"

if [ -n "$failures" ]; then
    printf '    x Красные фронтенды:%s\n' "$failures" >&2
    exit 1
fi

ok "Фронтенды зелёные."
