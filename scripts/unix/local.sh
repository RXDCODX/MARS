#!/usr/bin/env bash
#
# Пересобирает профиль запуска MARS.Local в src/*/Properties/launchSettings.json.
#
# Профиль описывает сервис, запущенный НА ХОСТЕ из IDE, при инфраструктуре в
# Docker. Без него локальный запуск не работает: в appsettings.json описан стенд
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
#
# Опции:
#   --check             только проверить: код возврата 1 при расхождении
#   --help              эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

check="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --check) check="yes"; shift ;;
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
