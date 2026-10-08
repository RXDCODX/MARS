#!/usr/bin/env bash
#
# Уборка после прогонов: артефакты сборки и мусор тестовых контейнеров.
#
# Две разные вещи, которые выглядят похоже, но требуют разных действий.
#
# Артефакты сборки (--action artifacts) — bin, obj, TestResults,
# coverage-local, publish, dist и ui-dist клиентов, .vs. Удаляются без
# вопросов: всё воспроизводимо. node_modules и .yarn не трогаются без
# --node-modules: их восстановление — это минуты и сеть.
#
# Мусор тестовых контейнеров (--action docker) — то, что
# PostgresTestDbContextFactory и MarsPostgres должны были убрать сами. Ryuk
# (resource reaper) — страховка, а не механизм удаления, и полагаться на него
# нельзя: на этой машине он остался в состоянии Created и не отработал ни
# разу. Поэтому скрипт сначала показывает, что осталось, и удаляет только по
# --remove.
#
# Про тома сказано отдельно, потому что это ловушка: образ postgres:16
# объявляет VOLUME /var/lib/postgresql/data, и этот том создаёт демон ДО
# контейнера. Меток контейнера в нём нет, Ryuk такой том не видит, и docker rm
# его не убирает. Testcontainers обходит это через tmpfs (WithTmpfsMount), но
# если монтирования не было, том уже создан. Удалять чужие висящие тома скрипт
# не будет: их много, и почти все — не наши.
#
# Примеры:
#   ./scripts/unix/clean.sh
#   ./scripts/unix/clean.sh --action docker
#   ./scripts/unix/clean.sh --action all --remove
#
# Опции:
#   --action artifacts|docker|all   что убрать (по умолчанию artifacts)
#   --remove                        удалить контейнеры, а не только показать
#   --node-modules                  удалить node_modules обоих клиентов
#   --help                          эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

action="artifacts"
remove="no"
node_modules="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --action) action="$2"; shift 2 ;;
        --remove) remove="yes"; shift ;;
        --node-modules) node_modules="yes"; shift ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

# remove_dir удаляет каталог и печатает, что именно. Путь не вычисляется от
# домашнего каталога: единственная цель — попасть внутрь репозитория, который
# уже найден по MARS.slnx.
remove_dir() {
    if [ -d "$1" ]; then
        rm -rf "$1"
        info "удалён $1"
    fi
}

clean_artifacts() {
    step "Артефакты сборки"

    remove_dir TestResults
    remove_dir coverage-local
    remove_dir coverage-report
    remove_dir publish
    remove_dir artifacts
    remove_dir .vs
    remove_dir src/MARS.Gateway/ClientApp/dist
    remove_dir src/MARS.Gateway/ClientApp/storybook-static
    remove_dir src/MARS.Gateway/ClientApp/coverage
    remove_dir src/MARS.MediaStorage/ui-dist

    # bin и obj — во всех проектах src/ и tests/.
    find src tests -maxdepth 2 -type d \( -name bin -o -name obj \) -print0 2>/dev/null |
        while IFS= read -r -d '' directory; do
            rm -rf "$directory"
            info "удалён $directory"
        done

    # tsbuildinfo разбросан по клиентам: путь кэша tsc абсолютный и машинный.
    find src -name '*.tsbuildinfo' -type f -print0 2>/dev/null |
        while IFS= read -r -d '' file; do
            rm -f "$file"
            info "удалён $file"
        done

    if [ "$node_modules" = "yes" ]; then
        remove_dir src/MARS.Gateway/ClientApp/node_modules
        remove_dir src/MARS.MediaStorage/ClientApp/node_modules
    else
        note "node_modules оставлены. Убрать: ./scripts/unix/clean.sh --node-modules"
    fi

    ok "Артефакты убраны. Следующая сборка будет полной."
}

clean_docker() {
    need_cmd docker "Нужен Docker, чтобы посмотреть оставшиеся контейнеры."

    step "Контейнеры тестов (метка org.testcontainers)"

    containers="$(docker ps -a --filter "label=org.testcontainers" \
        --format '{{.Names}}	{{.Status}}	{{.Image}}')"

    if [ -z "$containers" ]; then
        ok "Контейнеров с метками testcontainers нет — уборка тестов отработала."
    elif [ "$remove" = "yes" ]; then
        printf '%s\n' "$containers" | while IFS=$'\t' read -r name _ _; do
            [ -n "$name" ] || continue
            # -f, а не stop: удаление обязано быть синхронным, а остановленный
            # контейнер остаётся в docker ps -a и держит имя.
            docker rm -f "$name" >/dev/null
            info "удалён $name"
        done

        ok "Контейнеры удалены."
    else
        printf '%s\n' "$containers" | while IFS=$'\t' read -r name status image; do
            note "${name}	${status}	${image}"
        done

        note "Удалить: ./scripts/unix/clean.sh --action docker --remove"
    fi

    step "Висящие тома"

    dangling="$(docker volume ls -f dangling=true -q)"

    if [ -z "$dangling" ]; then
        ok "Висящих томов нет."
    else
        count="$(printf '%s\n' "$dangling" | wc -l | tr -d ' ')"
        note "Висящих томов: $count. Удалять их скрипт не будет: их много, и почти все — не наши."
        note "Свои тома стенда: docker volume ls | grep mars"
    fi
}

case "$action" in
    artifacts) clean_artifacts ;;
    docker) clean_docker ;;
    all)
        clean_artifacts
        clean_docker
        ;;
    *) fail "Неизвестное действие: $action." ;;
esac
