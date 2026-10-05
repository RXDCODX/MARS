#!/usr/bin/env bash
#
# Стенд на docker compose: поднять, погасить, посмотреть логи.
#
# Обёртка над compose с осмысленными действиями вместо длинных строк. Тонкости,
# которые она держит:
#
#   - docker-compose.dev.yml подключается только с --dev и вторым файлом. Он
#     переименован из override намеренно, чтобы обычный `docker compose up` не
#     подхватывал hot-reload (dotnet watch) молча;
#   - up всегда с --wait: compose возвращается, когда контейнеры запустились, а
#     не когда готовы. Без него следующий шаг (или разработчик) полезет в
#     Gateway, который ещё не слушает;
#   - .env.development (или .env.production без --dev) создаётся из своего шаблона,
#     если его нет: без него compose берёт значения по умолчанию, стенд
#     поднимается с пустыми паролями, а 01-databases.sh останавливается на первой
#     проверке. Существующий файл не перезаписывается — в нём настоящие секреты
#     стенда;
#   - down -v сносит тома, а без --volumes тома переживают стенд. Осторожно: на
#     томе mars-wwwroot живёт общий конвейер «алерт → файл» для obs, alerts и
#     media-storage.
#
# Наружу опубликован только Gateway (10155:8080), поэтому «зайти на стенд»
# означает http://localhost:10155. Grafana при этом на 30000, не на 3000: порт
# 3000 занят контейнером из чужого проекта.
#
# Примеры:
#   ./scripts/unix/stack.sh --action up
#   ./scripts/unix/stack.sh --action up --dev
#   ./scripts/unix/stack.sh --action logs --service gateway
#   ./scripts/unix/stack.sh --action down --volumes
#
# Опции:
#   --action up|down|restart|logs|ps|pull|config   что сделать (по умолчанию up)
#   --service ИМЯ                                    ограничить сервисами
#   --dev                                            подключить dev-оверлей
#   --build                                          пересобрать образы при up
#   --volumes                                        снести тома при down
#   --wait-timeout N                                 сколько ждать готовности
#   --tail N                                         строк логов
#   --help                                           эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

action="up"
services=""
dev="no"
build="no"
volumes="no"
wait_timeout=420
tail=200

while [ $# -gt 0 ]; do
    case "$1" in
        --action) action="$2"; shift 2 ;;
        --service) services="$services $2"; shift 2 ;;
        --dev) dev="yes"; shift ;;
        --build) build="yes"; shift ;;
        --volumes) volumes="yes"; shift ;;
        --wait-timeout) wait_timeout="$2"; shift 2 ;;
        --tail) tail="$2"; shift 2 ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd docker "Нужен Docker: стенд живёт в контейнерах."

if [ "$dev" = "yes" ]; then
    label="dev (hot-reload)"
else
    label="production"
fi

step "compose: $action, стенд $label"

# Файл среды нужен любой команде compose, а не только `up`: с --env-file compose
# падает с «couldn't find env file» на отсутствующем файле, то есть `logs` и `ps`
# перестают работать, когда среду не поднимали этой машине.
ensure_env_file "$dev"

case "$action" in
    up)
        # --wait и без --build тоже нужен: сервисы поднимаются по
        # service_healthy, и на холодном томе postgres это минуты.
        arguments=(up -d)

        [ "$build" = "yes" ] && arguments+=(--build)

        arguments+=(--wait --wait-timeout "$wait_timeout")
        ;;
    down)
        arguments=(down)

        if [ "$volumes" = "yes" ]; then
            note "Тома будут снесены: базы пересоздаются, mars-wwwroot очищается."
            arguments+=(-v)
        fi
        ;;
    restart) arguments=(restart) ;;
    logs) arguments=(logs --no-color --follow --tail "$tail") ;;
    ps) arguments=(ps) ;;
    pull) arguments=(pull) ;;
    config) arguments=(config) ;;
    *) fail "Неизвестное действие: $action." ;;
esac

# Имена сервисов идут после команды compose, а не перед ней:
# `docker compose -f … gateway up` — это «сервис gateway и подкоманда up», а
# compose ждёт подкоманду сразу после -f.
# shellcheck disable=SC2086
run_or_fail docker "compose $action" compose $(compose_args "$dev") "${arguments[@]}" $services

if [ "$action" = "up" ]; then
    ok "Стенд готов. Наружу открыт только Gateway: http://localhost:10155"
    info "Логи одного сервиса: ./scripts/unix/stack.sh --action logs --service gateway"
elif [ "$action" = "down" ]; then
    ok "Стенд погашен."
fi
