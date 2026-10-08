#!/usr/bin/env bash
#
# Навигационные тесты клиента: стенд целиком, от файла окружения до логов.
#
# Повторяет задачу e2e из ci.yml один в один. Каждый шаг здесь обязателен по
# причинам, которые уже стоили прогона:
#
#   - .env.production создаётся из .env.production.example: файлы окружений в git
#     не попадают, compose получает нужный флагом --env-file, а без файла стенд
#     поднимается со значениями по умолчанию, то есть с пустыми паролями, и
#     db-init останавливается на первой проверке;
#   - --wait обязателен: compose возвращается, когда контейнеры запустились, а
#     не когда они готовы, и тесты пошли бы в Gateway без клиента;
#   - --network host нужен, чтобы контейнер увидел Gateway на localhost
#     раннера: иначе localhost внутри контейнера — это сам контейнер;
#   - --shm-size=1g обязателен: 64 МБ /dev/shm не хватает Chromium, и он
#     падает с «Target crashed» без внятной причины;
#   - стенд гасится даже при упавших тестах: иначе логи будут не те.
#
# Образ тестов собирается здесь, а не в задаче сборки: тесты ходят в Gateway
# на localhost, а Playwright живёт внутри контейнера. Версия образа обязана
# совпадать с Microsoft.Playwright в Directory.Packages.props — Playwright
# сверяет свою версию с версией браузера и падает при расхождении.
#
# Примеры:
#   ./scripts/unix/e2e.sh
#   ./scripts/unix/e2e.sh --keep
#   ./scripts/unix/e2e.sh --skip-up
#
# Опции:
#   --keep           не гасить стенд после прогона
#   --skip-up        стенд уже поднят
#   --base-url URL   адрес стенда для Playwright (по умолчанию http://localhost:10155)
#   --wait-timeout N сколько секунд ждать готовности compose
#   --help           эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

keep="no"
skip_up="no"
base_url="http://localhost:10155"
wait_timeout=420

while [ $# -gt 0 ]; do
    case "$1" in
        --keep) keep="yes"; shift ;;
        --skip-up) skip_up="yes"; shift ;;
        --base-url) base_url="$2"; shift 2 ;;
        --wait-timeout) wait_timeout="$2"; shift 2 ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd docker "Нужен Docker: тесты ходят в Gateway, поднятый контейнерами."
need_cmd dotnet "Нужен .NET SDK — образ тестов собирается на SDK."

test_image="mars-clientui-tests"

step "Навигационные тесты клиента"

if [ "$skip_up" != "yes" ]; then
    ensure_env_file prod

    info "Подъём стенда: docker compose $(compose_args prod) up -d --build --wait"

    if ! run docker compose $(compose_args prod) up -d --build --wait --wait-timeout "$wait_timeout"; then
        printf '    x Стенд не поднялся. Логи:\n' >&2
        run docker compose $(compose_args prod) logs --no-color --tail 200 || true
        exit 1
    fi
else
    info "Стенд считается поднятым (--skip-up)"
fi

step "Образ тестов"

run_or_fail docker "Сборка образа тестов" \
    build -f tests/MARS.ClientUi.Tests/Dockerfile -t "$test_image" .

step "Прогон Playwright"

run_exit=0
run docker run --rm \
    --network host \
    --shm-size=1g \
    -e "MARS_CLIENTUI_BASE_URL=$base_url" \
    "$test_image" || run_exit=$?

step "Итог"

if [ "$keep" != "yes" ] && [ "$skip_up" != "yes" ]; then
    info "Гашение стенда: docker compose $(compose_args prod) down -v"
    run docker compose $(compose_args prod) down -v || true
elif [ "$keep" = "yes" ]; then
    info "Стенд оставлен поднятым: снять его — ./scripts/unix/stack.sh --action down --volumes"
fi

if [ "$run_exit" -ne 0 ]; then
    printf '    x Навигационные тесты красные (код %s).\n' "$run_exit" >&2

    if [ "$keep" != "yes" ] && [ "$skip_up" != "yes" ]; then
        note "Логи стенда уже сняты вместе с ним. Повторить с --keep, чтобы посмотреть."
    fi

    exit "$run_exit"
fi

ok "Навигационные тесты зелёные."
