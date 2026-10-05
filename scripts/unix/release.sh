#!/usr/bin/env bash
#
# Образы релиза: собрать, запушить в registry, поставить тег версии.
#
# Локальный аналог release-microservices.yml. Список образов не зашит в скрипт,
# а читается из матрицы publish того же workflow: единственное место, где
# решено, что публикуется. Зашитый список однажды разошёлся бы с матрицей — и
# молча, как это уже было со сводкой релиза, где client-ui и shikimori
# публиковались, но в итог не попадали.
#
# Три действия:
#
#   list    показать матрицу: имя образа, каталог, Dockerfile;
#   images  собрать образы (по умолчанию все). Без --push образы тегируются
#           как mars-<service> — так compose находит их локально; с --push имя
#           становится <registry>/<owner>/mars-<service>, как в workflow;
#   tag     поставить аннотированный git-тег vX.Y.Z и запушить его. Именно
#           пуш тега v* запускает публикацию образов в CI, поэтому публикация
#           руками и релизный тег — разные вещи, и скрипт их не смешивает.
#
# Набор тегов повторяет workflow: релизный тег, всегда sha-<короткий хеш>, и
# latest — только для семантической версии (^v[0-9]+(\.[0-9]+)*$). Иначе
# v1.0.0-rc1 стал бы прод-образом под именем latest.
#
# Финальный stage Dockerfile — final: он содержит curl, который нужен
# compose-healthcheck'ам (в mcr.microsoft.com/dotnet/aspnet его нет, иначе
# healthcheck даёт exit code 127 → unhealthy).
#
# Примеры:
#   ./scripts/unix/release.sh --action list
#   ./scripts/unix/release.sh --action images --service gateway --tag v1.2.3
#   ./scripts/unix/release.sh --action images --service gateway --tag v1.2.3 --push
#   ./scripts/unix/release.sh --action tag --version v1.2.3 --push
#
# Опции:
#   --action list|images|tag   что делать (по умолчанию list)
#   --service ИМЯ              сервис из матрицы; можно указать несколько раз
#   --tag ТЕГ                  тег образов (по умолчанию local)
#   --registry ХОСТ            registry (по умолчанию ghcr.io)
#   --owner ВЛАДЕЛЕЦ           владелец образа; по умолчанию из origin
#   --version vX.Y.Z           версия для действия tag
#   --push                     запушить образы или тег
#   --help                     эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

action="list"
services=""
tag="local"
registry="ghcr.io"
owner=""
version=""
push="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --action) action="$2"; shift 2 ;;
        --service) services="$services $2"; shift 2 ;;
        --tag) tag="$2"; shift 2 ;;
        --registry) registry="$2"; shift 2 ;;
        --owner) owner="$2"; shift 2 ;;
        --version) version="$2"; shift 2 ;;
        --push) push="yes"; shift ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

# is_semantic_version отвечает на вопрос «двигать ли latest». Регулярка та же,
# что в workflow: семантическая версия без суффикса.
is_semantic_version() {
    printf '%s' "$1" | grep -Eq '^v[0-9]+(\.[0-9]+)*$'
}

if [ "$action" = "list" ]; then
    step "Матрица публикации (release-microservices.yml)"
    show_release_services
    note "Сервисы, которых здесь нет, образов не получают: матрица выше и есть правда."
    exit 0
fi

need_cmd docker "Нужен Docker с buildx: сборка идёт multi-stage."

if [ "$action" = "tag" ]; then
    need_cmd git "Нужен git: тег ставится в репозитории."

    [ -n "$version" ] || fail "Действию tag нужен --version, например v1.2.3."

    if ! is_semantic_version "$version"; then
        fail "Тег '$version' не похож на vX.Y.Z. Публикация запускается только тегом v*, а latest двигается лишь семантической версией."
    fi

    step "Тег версии $version"

    # Грязное дерево запрещено намеренно: тег указывает на коммит, а не на
    # рабочую копию, и незакоммиченные изменения в релизе просто не появятся.
    status="$(git status --porcelain)"

    if [ -n "$status" ]; then
        fail "Рабочее дерево не чистое, тег не ставится. Сначала закоммитьте:
$status"
    fi

    if git rev-parse -q --verify "refs/tags/$version" >/dev/null; then
        fail "Тег '$version' уже есть. Повторный релиз под тем же тегом перетирает образы."
    fi

    if [ -n "$(git ls-remote --tags origin "refs/tags/$version")" ]; then
        fail "Тег '$version' уже есть в origin. Выпускать релиз под занятым тегом нельзя."
    fi

    run_or_fail git "Создание тега" tag -a "$version" -m "Релиз MARS"
    ok "Тег '$version' создан локально."

    if [ "$push" = "yes" ]; then
        note "Пуш тега '$version' в origin запустит release-microservices.yml и опубликует образы."
        run_or_fail git "Пуш тега" push origin "refs/tags/$version"
    else
        note "Тег не запушен. Публикации не будет, пока тег не уедет в origin."
    fi

    exit 0
fi

# images
targets="$(select_release_services "$services")"

short_sha="$(git rev-parse --short HEAD)" ||
    fail "Не удалось получить короткий хеш HEAD: репозиторий git не инициализирован?"

if [ -z "$owner" ]; then
    owner="$(git_owner)"

    if [ -z "$owner" ]; then
        if [ "$push" = "yes" ]; then
            fail "Не удалось определить владельца репозитория из origin. Задайте --owner явно."
        fi

        owner="local"
    fi
fi

step "Сборка образов, тег $tag"

if [ "$push" = "yes" ]; then
    info "Registry: $registry/$owner"
    info "Публикация идёт от вашей docker-сессии: docker login $registry обязателен заранее."
fi

failed=""
count=0

while IFS=$'\t' read -r service project dockerfile; do
    [ -n "$service" ] || continue

    count=$((count + 1))

    if [ "$push" = "yes" ]; then
        name="$registry/$owner/mars-$service"
    else
        name="mars-$service"
    fi

    info "$service: src/$project/$dockerfile"

    arguments=(build --file "src/$project/$dockerfile" --target final
        --tag "$name:$tag" --tag "$name:sha-$short_sha")

    if is_semantic_version "$tag"; then
        arguments+=(--tag "$name:latest")
    fi

    if [ "$push" = "yes" ]; then
        arguments+=(--push)
    fi

    arguments+=(".")

    exit=0
    run docker "${arguments[@]}" || exit=$?

    if [ "$exit" -ne 0 ]; then
        printf '    x %s не собрался (код %s)\n' "$service" "$exit" >&2
        failed="$failed $service"
    else
        ok "$service — $name:$tag, $name:sha-$short_sha"
    fi
done <<EOF
$targets
EOF

step "Итог"

if [ -n "$failed" ]; then
    printf '    x Не собраны:%s\n' "$failed" >&2
    exit 1
fi

if [ "$push" != "yes" ]; then
    note "Образы собраны локально. Запушить их можно отдельно: docker push <имя>:<тег>."
    note "Чтобы релиз появился в registry, нужен тег версии: ./scripts/unix/release.sh --action tag --version v1.2.3 --push"
else
    ok "Образы запушены в $registry/$owner."
fi
