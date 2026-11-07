#!/usr/bin/env bash
#
# Гейт перед пушем: форматирование, правило классов, сборка Release, тесты, фронтенд.
#
# То, что AGENTS.md требует перед `git push`: зелёные
# `dotnet build MARS.slnx -c Release` и `dotnet test MARS.slnx`, плюс
# `dotnet csharpier format .` и проверку одного верхнеуровневого класса на
# C#-файл через общий Roslyn-анализатор. Скрипт существует не для красоты: локальный
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
#   --parallel N                    сколько тестовых проектов гонять одновременно (1 — по очереди)
#   --max-class-violation-files N   сколько проблемных файлов показывать (0 — все, по умолчанию 20)
#   --skip-format-check             не проверять форматирование
#   --help                          эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

projects=""
frontend="no"
parallel=1
max_class_violation_files=20
skip_format="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --project) projects="$projects --project $2"; shift 2 ;;
        --frontend) frontend="yes"; shift ;;
        --parallel) parallel="$2"; shift 2 ;;
        --max-class-violation-files)
            [ $# -ge 2 ] || fail "Для --max-class-violation-files укажите число."
            case "$2" in
                ''|*[!0-9]*) fail "--max-class-violation-files должен быть неотрицательным числом." ;;
            esac
            max_class_violation_files="$2"
            shift 2
            ;;
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

print_summary() {
    if ! print_summary; then
    exit 1
fi

ok "Гейт пройден — можно коммитить и пушить в свою ветку."
