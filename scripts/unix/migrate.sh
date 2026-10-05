#!/usr/bin/env bash
#
# Миграции EF Core: добавить, убрать, обновить базу, посмотреть список.
#
# Фабрика проекта времени разработки лежит в src/MARS.X/Data/DesignTime, поэтому
# `dotnet ef` работает без запущенного стенда и без строк подключения в
# окружении. Проект без такой фабрики скрипт не примет: там `dotnet ef`
# попытался бы поднять хост приложения, то есть пошёл бы в сеть и в базу.
#
# dotnet-ef — глобальный инструмент, его нет в
# .config/dotnet-tools.json (в манифесте лежат только csharpier и
# reportgenerator). Проверка версии стоит первым шагом, иначе первая же команда
# падает с «не удаётся выполнить», то есть без указания, что доставить.
#
# Миграции применяются сервисом сами при старте — синхронно и до app.Run()
# (RunMarsSchemaMigrationsAsync), иначе фоновые службы успевают обратиться к
# несуществующим таблицам (42P01). Поэтому update нужен только чтобы проверить
# миграцию на живой базе, а не чтобы подготовить стенд.
#
# Примеры:
#   ./scripts/unix/migrate.sh --project MARS.TwitchCore --action add --name AddRewardHistory
#   ./scripts/unix/migrate.sh --project MARS.MediaStorage --action list
#   ./scripts/unix/migrate.sh --project MARS.CinemaQueue --action script --name Initial
#
# Опции:
#   --action add|remove|update|script|list   что сделать (по умолчанию list)
#   --project СЕРВИС                         сервис из src/ или путь к .csproj
#   --name ИМЯ                               имя миграции
#   --help                                   эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

action="list"
project=""
name=""

while [ $# -gt 0 ]; do
    case "$1" in
        --action) action="$2"; shift 2 ;;
        --project) project="$2"; shift 2 ;;
        --name) name="$2"; shift 2 ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd dotnet "Нужен .NET SDK версии из global.json (10.0.x)."

[ -n "$project" ] || fail "Нужен --project: имя сервиса из src/, например MARS.TwitchCore."

step "Миграции: $action, проект $project"

# dotnet-ef глобальный: `dotnet ef` без него просто не существует.
if ! dotnet ef --version >/dev/null 2>&1; then
    fail "dotnet-ef не установлен. Он глобальный и в .config/dotnet-tools.json его нет:
    dotnet tool install --global dotnet-ef"
fi

info "dotnet-ef: $(dotnet ef --version 2>&1 | tail -n 1)"

project_file="$(resolve_project_file "$project")"
project_name="$(basename "$project_file" .csproj)"

# Фабрика времени разработки — условие работоспособности dotnet ef. Без неё EF
# поднимает Program.cs, то есть ходит в сеть и в настоящие сервисы, и падение
# выглядит совсем не как «нет фабрики».
if ! find "src/$project_name" -name '*DbContextFactory.cs' -print -quit 2>/dev/null | grep -q .; then
    known="$(find src -path '*/Data/DesignTime/*DbContextFactory.cs' -print |
        sed -E 's,^src/([^/]+)/.*,\1,' | sort -u | tr '\n' ' ')"

    fail "В $project_name нет Data/DesignTime/*DbContextFactory.cs, и dotnet ef будет поднимать приложение целиком. Список сервисов с фабриками: $known"
fi

if { [ "$action" = "add" ] || [ "$action" = "script" ]; } && [ -z "$name" ]; then
    fail "Действию $action нужно --name: имя новой миграции либо миграция, с которой начинается скрипт."
fi

arguments=(ef migrations)

case "$action" in
    list) arguments+=(list) ;;
    remove) arguments+=(remove --project "$project_file") ;;
    update) arguments+=(update --project "$project_file") ;;
    script) arguments+=(script "$name" --project "$project_file") ;;
    add) arguments+=(add "$name" --project "$project_file") ;;
    *) fail "Неизвестное действие: $action." ;;
esac

if [ "$action" = "update" ]; then
    note "Update применит миграции к базе из строки подключения сервиса. Требуется поднятый postgres."
fi

run_or_fail dotnet "dotnet ef migrations $action" "${arguments[@]}"

ok "Готово."
info "Новые файлы миграций лежат в src/$project_name/Migrations — их нужно закоммитить в ту же задачу."
