#!/usr/bin/env bash
#
# Пересобирает профиль запуска MARS.Local в src/*/Properties/launchSettings.json.
#
# Профиль описывает сервис, запущенный НА ХОСТЕ из IDE (F5), при инфраструктуре
# в Docker. Без него локальный запуск не работает: в appsettings.json описан стенд
# контейнеров (RabbitMq:Host там rabbitmq), а контейнерные имена с хоста не
# резолвятся, и сервис падает на подключении к брокеру.
#
# Обёртка только находит корень репозитория и зовёт scripts/local_launch.py:
# реализация генератора одна на обе платформы намеренно, как гейт покрытия. Своя
# копия алгоритма на bash разошлась бы с версией для PowerShell молча — и
# разошлась бы именно в формате, который проверяет --check.
#
# Данные профиля — scripts/local-launch.conf: общий блок плюс блок сервиса.
# Подстановки ${ИМЯ} разрешает ШАБЛОН окружения (.env.development.example), а не
# файл с секретами: иначе настоящий токен уехал бы в коммит. Ключи из строк
# `#secrets` в профиль не попадают вовсе — их значения кладут в user-secrets.
#
# Использование:
#   ./scripts/unix/local.sh
#   ./scripts/unix/local.sh --check
#   ./scripts/unix/local.sh --run MARS.TwitchCore
#   ./scripts/unix/local.sh --run MARS.TwitchCore -- --urls http://localhost:9999
#
# Опции:
#   --check             только проверить: код возврата 1 при расхождении
#   --run СЕРВИС        после пересборки профиля поднять инфраструктуру и запустить
#                       сервис по профилю MARS.Local (СЕРВИС — каталог в src/)
#   --                  дальше идут аргументы самого сервиса
#   --help              эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

check="no"
service=""
service_args=()

while [ $# -gt 0 ]; do
    case "$1" in
        --check) check="yes"; shift ;;
        --run)
            if [ $# -lt 2 ]; then
                fail "Опция --run требует имя сервиса: каталог в src/, например MARS.TwitchCore."
            fi

            service="$2"
            shift 2
            ;;
        --) shift; service_args=("$@"); break ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

python="$(python_cmd)"

arguments=(scripts/local_launch.py --root .)

if [ "$check" = "yes" ]; then
    arguments+=(--check)
fi

"$python" "${arguments[@]}"

if [ -n "$service" ]; then
    project="src/$service/$service.csproj"

    if [ ! -f "$project" ]; then
        fail "Нет проекта src/$service. Имя сервиса — это каталог в src/ (например, MARS.TwitchCore)."
    fi

    step "Инфраструктура: $MARS_INFRA_SERVICES"

    # Паритет ключей нужен и здесь: поднимаем ту же среду, что и stack.sh, а
    # отставший файл окружения даёт случайный хост-порт — то есть сервис, который
    # молча пошёл не к тому стенду.
    if [ -f .env.development.example ]; then
        ensure_env_file dev
        test_env_file_parity "$environment" ".env.development.example"
    fi

    need_cmd docker "Нужен Docker: инфраструктура живёт в контейнерах."

    # Без базы и брокера сервис не стартует: миграции ждут базу ~50 с и падают, а
    # RabbitMQ пишет бесконечный реконнект. Поэтому фаза блокирующая: ждём именно
    # готовности, а не «контейнер запущен».
    # shellcheck disable=SC2086
    run_or_fail docker "compose up $MARS_INFRA_SERVICES" compose $(compose_args dev) \
        up -d --wait --wait-timeout "$MARS_INFRA_WAIT_TIMEOUT" $MARS_INFRA_SERVICES

    # Наблюдаемость — фоном и без --wait: недоступный образ Grafana не должен
    # срывать запуск сервиса, поэтому её отказ только отмечается.
    # shellcheck disable=SC2086
    if ! run docker compose $(compose_args dev) up -d $MARS_INFRA_BACKGROUND_SERVICES; then
        note "Наблюдаемость поднять не удалось: её отказ не мешает сервису."
    fi

    step "Запуск $service по профилю MARS.Local"

    dotnet_args=(run --project "$project" --launch-profile MARS.Local)

    if [ "${#service_args[@]}" -gt 0 ]; then
        # `--` отделяет аргументы сервиса от аргументов `dotnet run`.
        dotnet_args+=(-- "${service_args[@]}")
    fi

    exit_code=0
    run dotnet "${dotnet_args[@]}" || exit_code=$?

    exit "$exit_code"
fi
