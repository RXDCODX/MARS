#!/usr/bin/env bash
#
# Миграции EF Core: добавить, убрать, обновить базу, посмотреть список.
#
# Design-time factory (*DbContextFactory.cs) должна находиться в проекте,
# который владеет соответствующим DbContext, обычно под Data/DesignTime.
# Для разделённого сервиса скрипт принимает имя сервиса и находит среди его
# проектов тот, где есть factory. Поиск не привязан к старому суффиксу
# Infrastructure.Persistence.<Технология> и поддерживает новую форму
# Infrastructure.<Технология>. Без factory скрипт не примет проект: иначе
# dotnet ef поднял бы хост приложения и обратился бы к внешним сервисам/БД.
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

# Фабрика времени разработки — условие работоспособности dotnet ef.
# Без неё EF поднимает Program.cs, то есть ходит в сеть и в настоящие сервисы.
factory_found() {
    find "$1" -name '*DbContextFactory.cs' -print -quit 2>/dev/null | grep -q .
}

# Принимается конкретный .csproj/проект или имя сервиса. Если корневого
# проекта с таким именем больше нет, поиск слоёв всё равно работает по scope.
project_file=""
project_dir=""
project_name="$project"

case "$project" in
    *.csproj)
        project_file="$(resolve_project_file "$project")"
        project_dir="$(dirname "$project_file")"
        project_name="$(basename "$project_file" .csproj)"
        ;;
    *)
        if [ -f "src/$project/$project.csproj" ]; then
            project_file="src/$project/$project.csproj"
            project_dir="src/$project"
            project_name="$project"
        fi
        ;;
esac

if [ -n "$project_dir" ] && factory_found "$project_dir"; then
    :
else
    # Имя --project задаёт владельца сервиса, а не конкретный технический слой.
    # Ищем проекты владельца по префиксу и выбираем единственный с factory.
    projects_with_factory=()

    for candidate in src/"$project".*; do
        [ -d "$candidate" ] || continue
        candidate_name="$(basename "$candidate")"
        [ -f "$candidate/$candidate_name.csproj" ] || continue

        if factory_found "$candidate"; then
            projects_with_factory+=("$candidate")
        fi
    done

    if [ "${#projects_with_factory[@]}" -eq 1 ]; then
        project_dir="${projects_with_factory[0]}"
        project_name="$(basename "$project_dir")"
        project_file="$project_dir/$project_name.csproj"
        note "Миграции $project ведутся в проекте $project_name."
    elif [ "${#projects_with_factory[@]}" -gt 1 ]; then
        fail "Для $project найдено несколько проектов с DbContextFactory. Передай конкретный путь к .csproj."
    fi
fi

if [ -z "$project_dir" ] || ! factory_found "$project_dir"; then
    known="$(find src -path '*/Data/DesignTime/*DbContextFactory.cs' -print |
        sed -E 's,^src/([^/]+)/.*,\1,' | sort -u | tr '\n' ' ')"

    fail "В $project_name нет Data/DesignTime/*DbContextFactory.cs, и dotnet ef будет поднимать приложение целиком. Список сервисов с фабриками: $known"
fi

if { [ "$action" = "add" ] || [ "$action" = "script" ]; } && [ -z "$name" ]; then
    fail "Действию $action нужно --name: имя новой миграции либо миграция, с которой начинается скрипт."
fi

arguments=(ef migrations)

case "$action" in
    list) arguments+=(list --project "$project_file") ;;
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
