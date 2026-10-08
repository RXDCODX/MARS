#!/usr/bin/env bash
#
# Гейт перед пушем: форматирование, сборка Release, тесты, фронтенд.
#
# То, что AGENTS.md требует перед `git push`: зелёные
# `dotnet build MARS.slnx -c Release` и `dotnet test MARS.slnx`, плюс
# `dotnet csharpier format .`. Скрипт существует не для красоты: локальный
# прогон — единственный, кто отличает «красный новый тест» от «красного
# сломанного соседнего проекта», а CI отличает только второе.
#
# Шаги выполняются по очереди и останавливают скрипт на первом падении: тесты
# на несобранном коде дают результат, который нельзя интерпретировать. В конце
# печатается таблица с временем каждого шага — она отвечает на вопрос «что
# именно столько и ждало».
#
# Тесты по умолчанию идут по очереди, и это не осторожность, а разбор: каждый
# проект с базой поднимает свой контейнер Testcontainers, и параллельный прогон
# Solution на одной машине заканчивался сообщением «Foreground threads were
# left running, forcing process exit» при нуле красных тестов и коде 1. CI так не
# делает: там по одному проекту на задачу.
#
# Скрипт не коммитит и не пушит: это гейт, а не действие над историей.
#
# Примеры:
#   ./scripts/unix/verify.sh
#   ./scripts/unix/verify.sh --project MARS.Gateway.Tests --parallel 4
#   ./scripts/unix/verify.sh --frontend
#
# Опции:
#   --project ИМЯ        ограничить тесты подмножеством проектов
#   --frontend           добавить проверку клиента
#   --parallel N         сколько тестовых проектов гонять одновременно (1 — по очереди)
#   --skip-format-check  не проверять форматирование
#   --help               эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

projects=""
frontend="no"
parallel=1
skip_format="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --project) projects="$projects --project $2"; shift 2 ;;
        --frontend) frontend="yes"; shift ;;
        --parallel) parallel="$2"; shift 2 ;;
        --skip-format-check) skip_format="yes"; shift ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd dotnet "Нужен .NET SDK версии из global.json (10.0.x)."
need_cmd git "Нужен git: гейт сверяет ветку и состояние дерева."

summary=""
failures=""

step "Состояние репозитория"

branch="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || true)"

if [ "$branch" = "main" ] || [ "$branch" = "master" ]; then
    note "Мы на '$branch'. Пушить в main нельзя: работа идёт в отдельной ветке и завершается PR."
fi

if [ -n "$(git status --porcelain)" ]; then
    note "В рабочем дереве есть незакоммиченные изменения — гейт проверит не то, что уедет в PR."
fi

# record_step печатает строку итога. Результаты копятся в переменной, а не в
# массиве: шагов мало, а массив в bash 3.2 с append выглядел бы сложнее, чем
# сама таблица.
record_step() {
    local name="$1"
    local seconds="$2"
    local verdict="$3"

    summary="${summary}${name}|${seconds}|${verdict}
"
}

# run_step выполняет шаг, печатает его вердикт и пишет строку в итог. Код берётся
# из кода выхода, а не из факта запуска: дочерний скрипт внутри зовёт нативные
# команды и сам ничего не бросает.
run_step() {
    local name="$1"
    shift

    local started
    local seconds
    local exit=0

    step "$name"
    started="$(date +%s)"

    "$@" || exit=$?

    seconds=$(( $(date +%s) - started ))

    if [ "$exit" -eq 0 ]; then
        record_step "$name" "$seconds" "зелёный"
        return 0
    fi

    record_step "$name" "$seconds" "КРАСНЫЙ"
    printf '    x %s — код %s\n' "$name" "$exit" >&2
    failures="$failures $name"
    return "$exit"
}

# Шаги идут по очереди и каждый следующий имеет смысл только после
# предыдущего: тесты на красной сборке не проверяют ничего, а фронтенд на
# непроверенном бэкенде — тоже.
if [ "$skip_format" != "yes" ]; then
    format_exit=0
    run_step "Форматирование (CSharpier check)" \
        "$MARS_SCRIPT_DIR/format.sh" --action check || format_exit=$?

    if [ "$format_exit" -ne 0 ]; then
        note "Поправить: ./scripts/unix/format.sh"
    fi
fi

build_exit=0
run_step "Сборка Release" "$MARS_SCRIPT_DIR/build.sh" --action build || build_exit=$?

if [ "$build_exit" -eq 0 ]; then
    # shellcheck disable=SC2086
    test_exit=0
    run_step "Тесты" "$MARS_SCRIPT_DIR/test.sh" --parallel "$parallel" $projects ||
        test_exit=$?
fi

if [ "$frontend" = "yes" ]; then
    frontend_exit=0
    run_step "Клиент (типы и тесты)" "$MARS_SCRIPT_DIR/frontend.sh" --action check ||
        frontend_exit=$?
fi

step "Итог гейта"

printf '%s' "$summary" | while IFS='|' read -r name seconds verdict; do
    [ -n "$name" ] || continue
    printf '    %-34s %7s с  %s\n' "$name" "$seconds" "$verdict"
done

if [ -n "$failures" ]; then
    printf '    x Гейт не пройден:%s\n' "$failures" >&2
    exit 1
fi

ok "Гейт пройден — можно коммитить и пушить в свою ветку."
