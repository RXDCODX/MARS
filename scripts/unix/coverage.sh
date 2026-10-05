#!/usr/bin/env bash
#
# Локальный замер покрытия — теми же шагами, что задача coverage в CI.
#
# Шаги повторяют .github/workflows/ci.yml один в один, меняется только
# источник отчётов: вместо скачивания арфактов coverage-* их здесь порождают
# те же 17 матричных прогонов, только локально.
#
#   1. (по --skip-build) dotnet build MARS.slnx -c Release — иначе каждый прогон
#      сначала собирает своё, и время уходит впустую;
#   2. для каждого тестового проекта dotnet test с --coverlet — так же, как в
#      матрице tests, с теми же четырьмя фильтрами покрытия;
#   3. dotnet reportgenerator сливает отчёты в coverage-local/coverage-report;
#   4. .github/scripts/coverage-gate.py проверяет порог по слитому отчёту.
#
# Слияние шага 3 обязательно и локально: MARS.Shared (и почти каждый сервис)
# инструментируется в нескольких тестовых проектах, и простая сумма посчитала бы
# общие методы по разу на каждый проект.
#
# Фильтры покрытия повторяют те, что заданы в .github/scripts/coverage-local.ps1
# для Windows, и меняются вместе с ними:
#
#   coverlet_include         '[MARS.*]*'
#   coverlet_exclude         '[*.Test*]*'
#   coverlet_exclude_by_file '**/Migrations/**'
#
# Без include coverlet берёт и чужие сборки (HealthChecks, YARP, Serilog), и
# доля кода репозитория в отчёте становится нечитаемой. Один шаблон
# '[*.Test*]*', а не список через «;»: coverlet.MTP из двух шаблонов применяет
# только первый, и MARS.TestKit остался бы в отчёте. Миграции EF генерируются,
# а не пишутся руками, и тестами не закрываются.
#
# Пустой набор отчётов считается ошибкой, а не нулём процентов: сломанная
# выгрузка не должна давать зелёный статус при нулевом покрытии.
#
# Примеры:
#   ./scripts/unix/coverage.sh                                полный честный замер
#   ./scripts/unix/coverage.sh --project MARS.OBS.Tests --no-gate
#   ./scripts/unix/coverage.sh --skip-tests                   пересобрать слияние
#   ./scripts/unix/coverage.sh --parallel 4
#
# Опции:
#   --project ИМЯ         ограничить прогон проектами (список через запятую)
#   --skip-build          не собирать решение перед прогоном
#   --skip-tests          не гонять тесты, пересобрать только слияние и порог
#   --parallel N          сколько проектов гонять одновременно (по умолчанию 8)
#   --threshold-methods N порог по методам, в процентах (по умолчанию 95)
#   --output-dir ПУТЬ     куда складывать отчёты (по умолчанию coverage-local)
#   --no-gate            не проверять порог
#   --help                эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

requested=""
skip_build="no"
skip_tests="no"
parallel=8
threshold=95
output_dir="coverage-local"
no_gate="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --project) requested="$requested $2"; shift 2 ;;
        --skip-build) skip_build="yes"; shift ;;
        --skip-tests) skip_tests="yes"; shift ;;
        --parallel) parallel="$2"; shift 2 ;;
        --threshold-methods) threshold="$2"; shift 2 ;;
        --output-dir) output_dir="$2"; shift 2 ;;
        --no-gate) no_gate="yes"; shift ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd dotnet "Нужен .NET SDK версии из global.json (10.0.x)."
python="$(python_cmd)"

gate_script=".github/scripts/coverage-gate.py"

[ -f "$gate_script" ] || fail "Не найден гейт покрытия: $gate_script"

# Те же значения, что в ci.yml. Дублируются намеренно: если их поменять в одном
# месте и забыть про другое, локальное число перестанет быть сравнимым с CI, а
# это худший вид расхождения — тихий.
coverlet_include='[MARS.*]*'
coverlet_exclude='[*.Test*]*'
coverlet_exclude_by_file='**/Migrations/**'

results_root="$output_dir/TestResults"
reports_root="$output_dir/reports"
report_dir="$output_dir/coverage-report"

# --- Тестовые проекты ------------------------------------------------------

discovered="$(test_projects)"

[ -n "$discovered" ] || fail "В tests/ не найдено ни одного проекта. Проверьте корень."

info "Тестовых проектов в tests/: $(printf '%s\n' "$discovered" | wc -l | tr -d ' ')"

targets="$discovered"

if [ -n "$requested" ]; then
    targets=""

    for name in $(printf '%s' "$requested" | tr ',' ' '); do
        targets="$targets
$(resolve_test_project "$name")"
    done

    targets="$(printf '%s' "$targets" | sed '/^$/d')"

    info "Из них в прогоне: $(printf '%s\n' "$targets" | wc -l | tr -d ' ') ($targets)"
fi

info "Порог покрытия методами: $threshold%"

# --- Сверка с матрицей CI --------------------------------------------------
# Расхождение в любую сторону — ошибка: и отсутствующий в матрице проект, и
# проект-призрак в матрице означают, что локальное покрытие и покрытие в CI
# считаются для разного набора кода.
in_matrix="$(grep -E '^[[:space:]]*-[[:space:]]+MARS\.[A-Za-z0-9.]*\.Tests[[:space:]]*$' \
    .github/workflows/ci.yml | sed -E 's/^[[:space:]]*-[[:space:]]+//; s/[[:space:]]*$//' | sort -u)"

missing_in_matrix="$(printf '%s\n' "$discovered" | while read -r name; do
    [ -n "$name" ] || continue
    printf '%s\n' "$in_matrix" | grep -qx -- "$name" || printf '%s\n' "$name"
done)"

missing_on_disk="$(printf '%s\n' "$in_matrix" | while read -r name; do
    [ -n "$name" ] || continue
    printf '%s\n' "$discovered" | grep -qx -- "$name" || printf '%s\n' "$name"
done)"

if [ -n "$missing_in_matrix" ]; then
    fail "Тестовый проект есть в tests/, но его нет в матрице tests в ci.yml: $(printf '%s ' $missing_in_matrix). Без записи в матрицу он не проверяется в CI вообще, а его покрытие не попадёт в слитый отчёт."
fi

if [ -n "$missing_on_disk" ]; then
    fail "Матрица tests в ci.yml ссылается на несуществующий проект: $(printf '%s ' $missing_on_disk). Задача упадёт на restore."
fi

# --- Сборка ----------------------------------------------------------------

if [ "$skip_build" != "yes" ]; then
    step "Сборка решения"
    run_or_fail dotnet "Сборка решения" build MARS.slnx -c Release
fi

# --- Тесты с покрытием -----------------------------------------------------

status_file="$(mktemp)"

if [ "$skip_tests" != "yes" ]; then
    # Старые отчёты снимаем, иначе в слияние попадёт прошлый замер проекта,
    # который в этом прогоне почему-то не отдал свой.
    rm -rf "$results_root"
    mkdir -p "$results_root"

    step "Тесты с покрытием (параллельно $parallel)"

    run_covered_project() {
        local name="$1"
        local configuration="$2"
        local results="$3"
        local verdict=0

        mkdir -p "$results/$name"

        # Результат в отдельный лог: на красном проекте его лог и есть то, что
        # нужно смотреть, а в общий поток оно не попадёт.
        set +e
        dotnet test "tests/$name/$name.csproj" \
            -c "$configuration" \
            --no-build \
            --results-directory "$results/$name" \
            -- \
            --coverlet \
            --coverlet-output-format cobertura \
            --coverlet-file-prefix "$name" \
            --coverlet-include "$COVERLET_INCLUDE" \
            --coverlet-exclude "$COVERLET_EXCLUDE" \
            --coverlet-exclude-by-file "$COVERLET_EXCLUDE_BY_FILE" \
            --report-xunit-trx \
            >"$results/$name/test.log" 2>&1 || verdict=$?
        set -e

        printf '%s\t%s\n' "$name" "$verdict" >>"$status_file"
    }

    # Переменные фильтров экспортируются, потому что воркер запускается
    # отдельным bash: неэкспортированная переменная в дочернем процессе была бы
    # пустой, и coverlet снял бы отчёт по всему подряд — то есть зелёный
    # результат, измеряющий чужой код.
    export COVERLET_INCLUDE="$coverlet_include"
    export COVERLET_EXCLUDE="$coverlet_exclude"
    export COVERLET_EXCLUDE_BY_FILE="$coverlet_exclude_by_file"
    export status_file
    export -f run_covered_project

    printf '%s\n' "$targets" |
        xargs -P "$parallel" -I{} bash -c 'run_covered_project "$@"' _ {} Release "$results_root" ||
        fail "Параллельный прогон оборвался. Логи: $results_root"

    failed=""

    while read -r name verdict; do
        case "$verdict" in
            0) ok "$name — зелёные" ;;
            *)
                printf '    x %s — КРАСНЫЕ, лог: %s/%s/test.log\n' "$name" "$results_root" "$name" >&2
                failed="$failed $name"
                ;;
        esac
    done <"$status_file"

    rm -f "$status_file"

    if [ -n "$failed" ]; then
        fail "Упали тестовые проекты: $(printf '%s ' $failed). Покрытие на упавших тестах мерять рано — сперва чиним тесты."
    fi

    printf '\n'
fi

# --- Сбор отчётов по проектам ----------------------------------------------

rm -rf "$reports_root"
mkdir -p "$reports_root"

found="$(find "$results_root" -name '*.cobertura*.xml' 2>/dev/null || true)"

if [ -z "$found" ]; then
    fail "Не найдено ни одного отчёта cobertura в $results_root. Нулевой набор отчётов — это ошибка выгрузки, а не нулевое покрытие: проверьте, что в тестовых проектах есть PackageReference coverlet.MTP."
fi

for report in $found; do
    cp -f "$report" "$reports_root/$(basename "$report")"
done

info "Отчётов cobertura: $(printf '%s\n' "$found" | wc -l | tr -d ' ')"

# --- Слияние ---------------------------------------------------------------

step "Слияние отчётов"

rm -rf "$report_dir"

# ReportGenerator лежит в локальном манифесте .config/dotnet-tools.json, а не
# в системном наборе: на свежей клоне `dotnet reportgenerator` не найдётся, и
# замер падал бы на «инструмент не установлен» вместо покрытия.
dotnet_tool_restore

run_or_fail dotnet "ReportGenerator" reportgenerator \
    "-reports:$reports_root/**/*.cobertura*.xml" \
    "-targetdir:$report_dir" \
    "-reporttypes:HtmlInline_AzurePipelines_Dark;Cobertura;TextSummary"

# --- Порог -----------------------------------------------------------------

if [ -n "$requested" ]; then
    note "Прогон по подмножеству: покрытие в слитом отчёте ниже, чем в CI, и проценты несравнимы."
fi

step "Проверка порога"

gate_exit=0
run "$python" "$gate_script" \
    --merged "$report_dir/Cobertura.xml" \
    --reports-dir "$reports_root" \
    --reports-pattern '*.cobertura*.xml' \
    --threshold-methods "$threshold" || gate_exit=$?

if [ "$no_gate" = "yes" ]; then
    note "Порог не проверялся (--no-gate)"
    exit 0
fi

if [ "$gate_exit" -ne 0 ]; then
    printf '\n'
    note "Порог не пройден. Список самых непокрытых классов:"
    run "$python" .github/scripts/coverage-gaps.py --merged "$report_dir/Cobertura.xml" --top 40 || true
    exit 1
fi

ok "Гейт покрытия пройден."
