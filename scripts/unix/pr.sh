#!/usr/bin/env bash
#
# Ветка, пуш и Pull Request — по порядку, с гейтом между шагами.
#
# Работа в этом репозитории идёт так: отдельная ветка, зелёные сборка и тесты
# локально, пуш своей ветки, PR с заполненным описанием. Скрипт выполняет
# последовательность и НЕ делает две вещи: не коммитит (коммит — это логический
# шаг, решает человек) и не мержит (merge — решение владельца).
#
# Что скрипт добавляет к описанию в AGENTS.md:
#
#   - отказывается работать на main — пуш в main запрещён;
#   - прогоняет verify.sh перед пушем, если не сказано иное;
#   - запускает `gh workflow run ci.yml --ref <ветка>`. Push, сделанный
#     GITHUB_TOKEN, не запускает workflow (защита GitHub от рекурсии), поэтому
#     обязательные статусы в branch protection иначе не закрылись бы никогда;
#   - требует текст описания: пустой PR без описания не считается сделанной
#     работой, а формальные разделы хуже отсутствующих.
#
# Примеры:
#   ./scripts/unix/pr.sh --print-template
#   ./scripts/unix/pr.sh --title 'feat: RANDOM ART через matoi' --body-file pr.md
#
# Опции:
#   --title ЗАГОЛОВОК       conventional-коммит по-русски
#   --body ТЕКСТ            описание PR
#   --body-file ФАЙЛ        описание PR из файла
#   --base ВЕТКА            ветка назначения (по умолчанию main)
#   --skip-checks           не прогонять гейт перед пушем
#   --skip-workflow         не перезапускать CI на ветке
#   --print-template        напечатать шаблон описания и выйти
#   --help                  эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

template=$(cat <<'TEMPLATE'
## Что
<одно предложение: какое поведение изменилось>

## Зачем
<проблема или требование, из которого это следует; ссылка на дефект или задачу>

## Как
<ключевые решения и почему именно так>

## Проверка
<имя тест-проекта и класс/метод теста либо ручной сценарий с командой>

## Миграция и откат
<что нужно владельцу: новые переменные в .env.production (или .env.development), пересоздание тома, перезапуск стенда; как откатить>

## Сквозные правки
<.env.production.example и .env.development.example, compose, Directory.Packages.props, ci.yml, ServiceEndpoints, README, миграции>
TEMPLATE
)

title=""
body=""
body_file=""
base="main"
skip_checks="no"
skip_workflow="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --title) title="$2"; shift 2 ;;
        --body) body="$2"; shift 2 ;;
        --body-file) body_file="$2"; shift 2 ;;
        --base) base="$2"; shift 2 ;;
        --skip-checks) skip_checks="yes"; shift ;;
        --skip-workflow) skip_workflow="yes"; shift ;;
        --print-template) printf '%s\n' "$template"; exit 0 ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd git "Нужен git: скрипт работает с веткой и пушем."
need_cmd gh "Нужен GitHub CLI (gh): PR создаётся через gh pr create."

branch="$(git rev-parse --abbrev-ref HEAD)" ||
    fail "Не git-репозиторий или нет HEAD: $PWD"

if [ "$branch" = "main" ] || [ "$branch" = "master" ]; then
    fail "Мы на '$branch'. Работа идёт в отдельной ветке: создайте её до первой правки (git switch -c feat/короткое-имя) и повторите."
fi

[ -n "$title" ] ||
    fail "Нужен --title: conventional-коммит по-русски, например 'feat: RANDOM ART через matoi'."

if [ -n "$body_file" ]; then
    [ -f "$body_file" ] || fail "Файл с описанием не найден: $body_file"
    body="$(cat "$body_file")"
fi

if [ -z "$body" ]; then
    step "Шаблон описания PR"
    printf '%s\n' "$template"
    fail "Описание обязательно: заполните --body или --body-file. Пустой PR не считается сделанной работой."
fi

step "Ветка $branch -> $base"

# PR мог быть открыт раньше; gh pr create тогда падает, а состояние ветки всё
# равно полезно проверить.
existing="$(gh pr view --json url -q .url 2>/dev/null || true)"

if [ -n "$existing" ]; then
    note "PR уже открыт: $existing"
fi

if [ "$skip_checks" != "yes" ]; then
    info "Гейт перед пушем: ./scripts/unix/verify.sh"

    gate_exit=0
    "$MARS_SCRIPT_DIR/verify.sh" || gate_exit=$?

    if [ "$gate_exit" -ne 0 ]; then
        fail "Гейт не пройден — пуш не делаем. Поправить и повторить."
    fi
fi

step "Пуш ветки"

run_or_fail git "Пуш ветки $branch" push -u origin "$branch"

if [ "$skip_workflow" != "yes" ]; then
    info "Перезапуск CI на ветке: gh workflow run ci.yml --ref $branch"
    run gh workflow run ci.yml --ref "$branch" || true
fi

if [ -n "$existing" ]; then
    ok "PR уже был открыт: $existing"
    exit 0
fi

step "Создание PR"

run_or_fail gh "Создание PR" pr create --base "$base" --head "$branch" --title "$title" --body "$body"

url="$(gh pr view --json url -q .url 2>/dev/null || true)"

if [ -n "$url" ]; then
    ok "PR: $url"
    note "Merge — решение владельца: gh pr merge скриптом не вызывается."
fi
