#!/usr/bin/env bash
#
# Поиск хвостов: старое имя, переменная или порт по всему репозиторию.
#
# Самая частая ошибка этого репозитория — переименовать или выпилить что-то в
# «своих» файлах и оставить хвосты в остальных. Собственный diff этого не
# показывает: шаблоны .env.*.example просто не входят в список изменённых, а
# стенд при этом работает. Так терялись SEQ_ADMIN_PASSWORD, Loki__Url и LogsDb
# при выпиливании Seq, и переменные окружения при переходе Jaeger → Tempo.
#
# Скрипт ищет по файлам, а не по git grep: git grep смотрит только
# индексированные файлы, а новые в индекс не попали — на этой машине
# `git grep --untracked` возвращал 0 совпадений вместо того, чтобы добавить
# неотслеживаемые.
#
# Совпадения после прогона бывают трёх видов, и это нормально:
#
#   - объясняющий комментарий («раньше здесь был…») — оставить;
#   - «почему так» в README.md / AGENTS.md — оставить;
#   - рабочая ссылка: переменная окружения, имя сервиса, порт, путь, имя образа —
#     удалить.
#
# Примеры:
#   ./scripts/unix/sweep.sh --term SEQ_ADMIN_PASSWORD
#   ./scripts/unix/sweep.sh --term 3000 --term 9155
#   ./scripts/unix/sweep.sh --term Loki__Url --case-sensitive
#
# Опции:
#   --term ИМЯ          что искать; можно указать несколько раз
#   --path ПУТЬ         где искать (по умолчанию .)
#   --case-sensitive    учитывать регистр
#   --help              эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

terms=""
path="."
case_sensitive="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --term) terms="$terms|$2"; shift 2 ;;
        --path) path="$2"; shift 2 ;;
        --case-sensitive) case_sensitive="yes"; shift ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

[ -n "$terms" ] || fail "Нужен --term: что искать. Например --term SEQ_ADMIN_PASSWORD."

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

patterns=(
    '*.cs' '*.json' '*.yml' '*.yaml' '*.md' '*.props' '*.csproj'
    '*.ps1' '*.sh'
    # .env*, а не *.env: файлы окружений названы .env.development и
    # .env.production, и старый шаблон их не видел — мёртвая переменная в
    # боевом окружении всплыла бы только при переименовании.
    '.env*' '*.example' '*.alloy' '*.slnx' '*.ts'
    '*.tsx' '*.js' '*.sql' 'Dockerfile' '*.conf' '*.txt' '*.cshtml'
)

step "Sweep: ${terms#|}"

files="$(search_files "$path" "${patterns[@]}")"
info "Файлов к проверке: $(printf '%s\n' "$files" | grep -c . || true)"

if [ "$case_sensitive" = "yes" ]; then
    grep_flags="-s -n -F"
else
    grep_flags="-s -n -i -F"
fi

total=0
list="$(printf '%s' "$terms" | tr '|' '\n')"

while IFS= read -r term; do
    [ -n "$term" ] || continue

    step "Ищем: $term"

    found="$(printf '%s\n' "$files" | xargs -r grep $grep_flags -- "$term" 2>/dev/null || true)"

    if [ -z "$found" ]; then
        ok "Совпадений нет."
        continue
    fi

    printf '%s\n' "$found" | cut -c1-180
    count="$(printf '%s\n' "$found" | grep -c . || true)"
    total=$((total + count))

    note "Совпадений: $count. Разберите каждое: рабочая ссылка удаляется, объяснение остаётся."
done <<EOF
$list
EOF

step "Итог: совпадений $total"

if [ "$total" -eq 0 ]; then
    ok "Чисто."
fi
