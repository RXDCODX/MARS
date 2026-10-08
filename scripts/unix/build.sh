#!/usr/bin/env bash
#
# Сборка решения, restore и publish — теми же командами, что и в CI.
#
# Точное соответствие обязательно: `dotnet build MARS.slnx -c Release` из
# ci.yml и локальный `dotnet build -c Debug` проверяют разное. В частности,
# предупреждения компиляции становятся ошибками только в Release:
# TreatWarningsAsErrors задан в каждом .csproj, а Debug-сборка CI не
# проверяет.
#
# Примеры:
#   ./scripts/unix/build.sh                                  собрать всё решение
#   ./scripts/unix/build.sh --project MARS.Gateway --configuration Debug
#   ./scripts/unix/build.sh --action publish --project MARS.Gateway
#
# Опции:
#   --action restore|build|publish   что делать (по умолчанию build)
#   --project ИМЯ                    каталог сервиса из src/ или путь к .csproj
#   --configuration Debug|Release    конфигурация (по умолчанию Release)
#   --no-restore                     не восстанавливать пакеты
#   --help                           эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

action="build"
project=""
configuration="Release"
no_restore="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --action)
            action="$2"
            shift 2
            ;;
        --project)
            project="$2"
            shift 2
            ;;
        --configuration)
            configuration="$2"
            shift 2
            ;;
        --no-restore) no_restore="yes"; shift ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd dotnet "Нужен .NET SDK версии из global.json (10.0.x)."

if [ "$action" = "publish" ] && [ -z "$project" ]; then
    fail "Для publish нужен --project: публиковать решение целиком нечего."
fi

step "Действие: $action, конфигурация: $configuration"

if [ "$action" = "restore" ]; then
    run_or_fail dotnet "Восстановление пакетов" restore MARS.slnx
    exit 0
fi

if [ "$action" = "build" ]; then
    # Отдельный restore перед сборкой нужен не для красоты: без него
    # `dotnet build` сам запускает restore, но неявно, и его ошибка выглядит
    # как ошибка компиляции — а это разные диагностики.
    if [ "$no_restore" != "yes" ]; then
        info "Восстановление пакетов"
        run_or_fail dotnet "Восстановление пакетов" restore MARS.slnx
    fi

    info "Сборка MARS.slnx"

    if [ "$no_restore" = "yes" ]; then
        run_or_fail dotnet "Сборка решения" build MARS.slnx -c "$configuration" --no-restore
    else
        run_or_fail dotnet "Сборка решения" build MARS.slnx -c "$configuration"
    fi

    ok "Решение собрано."
    exit 0
fi

if [ "$action" = "publish" ]; then
    project_file="$(resolve_project_file "$project")"
    project_name="$(basename "$project_file" .csproj)"

    info "Публикация $project_name в publish/$project_name"

    if [ "$no_restore" = "yes" ]; then
        run_or_fail dotnet "Публикация $project_name" \
            publish "$project_file" -c "$configuration" -o "publish/$project_name" --no-restore
    else
        run_or_fail dotnet "Публикация $project_name" \
            publish "$project_file" -c "$configuration" -o "publish/$project_name"
    fi

    ok "Опубликовано в publish/$project_name (каталог publish/ в .gitignore)."
    exit 0
fi

fail "Неизвестное действие: $action. Ожидалось restore, build или publish."
