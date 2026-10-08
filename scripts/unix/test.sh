#!/usr/bin/env bash
#
# Прогон тестов с фильтрами Microsoft.Testing.Platform.
#
# Обёртка нужна из-за двух ловушек, обе проверены на этой машине.
#
# Фильтры. global.json включает Microsoft.Testing.Platform, а проекты на
# xunit.v3 имеют OutputType=Exe. Фильтр VSTest-вида `--filter` молча находит
# ноль тестов и выходит с кодом 8, поэтому опции тест-приложения передаются
# после `--`, а имена параметров здесь именно MTP-овские: --filter-class,
# --filter-method, --filter-namespace, --filter-trait.
#
# Ноль тестов при зелёной сборке. На SDK 10.0.401 интеграция dotnet test с
# MTP отдавала «Запущено ноль тестов» и код 5, тогда как запуск того же dll
# через dotnet exec выполнял все тесты. Если сборка и сам тест-проект
# собираются, а прогон не нашёл ни одного теста — это оно, а не сломанный
# тест. --use-test-host запускает dll напрямую и обходит интеграцию.
#
# Общая сборка решения выполняется один раз до прогонов, а сами тесты идут с
# --no-build: иначе каждый проект собирает решение заново, и время уходит не в
# тесты.
#
# При --parallel N вывод каждого проекта пишется в свой лог
# (artifacts/test-logs/<проект>.log), иначе выводы разных `dotnet test`
# перемешались бы в один поток, где красный проект не найти.
#
# По умолчанию проекты идут по очереди, и это не осторожность, а разбор: при
# параллельном прогоне Solution на одной машине тестовые процессы
# завершались с «[FATAL ERROR] Foreground threads were left running, forcing
# process exit» — при нуле красных тестов и коде 1. Причина в том, что каждый
# проект с базой поднимает свой контейнер Testcontainers. CI так не делает:
# там по одному проекту на задачу.
#
# Примеры:
#   ./scripts/unix/test.sh --project MARS.Gateway.Tests
#   ./scripts/unix/test.sh --project MARS.Shared.Tests --filter-class '*HealthCheck*'
#   ./scripts/unix/test.sh --project MARS.TwitchCore.Tests --filter-method '*Foo.Bar*'
#   ./scripts/unix/test.sh --project MARS.Admin.Tests --use-test-host
#   ./scripts/unix/test.sh --parallel 8
#
# Опции:
#   --project ИМЯ            тестовый проект; можно указать несколько раз
#   --configuration КОНФИГ   Debug или Release (по умолчанию Release)
#   --no-build               не собирать решение перед прогоном
#   --use-test-host          запускать dll через dotnet exec
#   --filter-class ШАБЛОН   фильтр по классу
#   --filter-method ШАБЛОН  фильтр по методу
#   --filter-namespace ШАБЛОН
#   --filter-trait КЛЮЧ=ЗНАЧЕНИЕ
#   --parallel N             сколько проектов гонять одновременно (1 — по очереди)
#   --help                   эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

configuration="Release"
no_build="no"
use_test_host="no"
filter_class=""
filter_method=""
filter_namespace=""
filter_trait=""
parallel=1
requested=""

while [ $# -gt 0 ]; do
    case "$1" in
        --project) requested="$requested $2"; shift 2 ;;
        --configuration) configuration="$2"; shift 2 ;;
        --no-build) no_build="yes"; shift ;;
        --use-test-host) use_test_host="yes"; shift ;;
        --filter-class) filter_class="$2"; shift 2 ;;
        --filter-method) filter_method="$2"; shift 2 ;;
        --filter-namespace) filter_namespace="$2"; shift 2 ;;
        --filter-trait) filter_trait="$2"; shift 2 ;;
        --parallel) parallel="$2"; shift 2 ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

case "$parallel" in
    ''|*[!0-9]*) fail "--parallel требует число от 1 до 32." ;;
esac

if [ "$parallel" -lt 1 ] || [ "$parallel" -gt 32 ]; then
    fail "--parallel требует число от 1 до 32."
fi

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd dotnet "Нужен .NET SDK версии из global.json (10.0.x)."

if [ -n "$requested" ]; then
    targets=""
    for name in $requested; do
        targets="$targets $(resolve_test_project "$name")"
    done
else
    targets="$(test_projects | tr '\n' ' ')"
fi

# --use-test-host требует собранный проект: dll запускается напрямую.
if [ "$use_test_host" = "yes" ]; then
    no_build="yes"
fi

if [ "$no_build" != "yes" ]; then
    step "Сборка решения ($configuration)"
    run_or_fail dotnet "Сборка решения" build MARS.slnx -c "$configuration"
fi

# Фильтры собираются в массив, а не в строку: значение может содержать
# пробелы и кавычки ('*Health Check*'), и разбиение такой строки на слова
# разъехало бы фильтр на два аргумента.
filter_args=()

[ -n "$filter_class" ] && filter_args+=(--filter-class "$filter_class")
[ -n "$filter_method" ] && filter_args+=(--filter-method "$filter_method")
[ -n "$filter_namespace" ] && filter_args+=(--filter-namespace "$filter_namespace")
[ -n "$filter_trait" ] && filter_args+=(--filter-trait "$filter_trait")

step "Тесты, конфигурация $configuration"

if [ "${#filter_args[@]}" -gt 0 ]; then
    info "Фильтр: ${filter_args[*]}"
fi

status_file="$(mktemp)"
log_root="artifacts/test-logs"
mkdir -p "$log_root"

# run_project гоняет один проект и возвращает вердикт: 0 — зелёный, 1 —
# красный, 2 — тесты не нашлись. Вердикт отдельным кодом, потому что «не
# нашлись» и «красные» требуют разных действий: первое — поправить фильтр,
# второе — чинить тест.
run_project() {
    local name="$1"
    local arguments=()
    local exit=0

    if [ "$use_test_host" = "yes" ]; then
        arguments=(exec "tests/$name/bin/$configuration/net10.0-windows/$name.dll")
    else
        arguments=(test "tests/$name/$name.csproj" -c "$configuration" --no-build)
    fi

    if [ "${#filter_args[@]}" -gt 0 ]; then
        if [ "$use_test_host" = "yes" ]; then
            arguments+=("${filter_args[@]}")
        else
            # Один аргумент «--»: тест-приложение отделяется от dotnet test
            # именно им, а не пустым местом в списке.
            arguments+=("--" "${filter_args[@]}")
        fi
    fi

    run dotnet "${arguments[@]}" || exit=$?

    if [ "$exit" -eq 0 ]; then
        ok "$name — зелёные"
        return 0
    fi

    if [ "$exit" -eq 5 ] || [ "$exit" -eq 8 ]; then
        note "Код $exit от тест-приложения означает, что тесты не нашлись, а не что они красные."
        note "Проверьте имя класса и проекта: фильтр чувствителен к регистру."
        note "Обойти интеграцию dotnet test с MTP: --use-test-host."
        return 2
    fi

    printf '    x %s — красные (код %s)\n' "$name" "$exit" >&2
    return 1
}

# run_project_reported — то же для параллельного режима: вердикт пишется в
# общий файл статусов, потому что сообщения из фоновых процессов перемешались
# бы с итогом. Все настройки приходят аргументами, а не переменными окружения:
# экспортировать массив фильтров bash 3.2 не умеет, и молча пустой фильтр —
# это прогон всех тестов вместо одного класса.
run_project_reported() {
    local name="$1"
    shift

    local configuration="$1"
    shift

    local use_test_host="$1"
    shift

    local log_root="$1"
    shift

    local status_file="$1"
    shift

    local filter_args=("$@")
    local arguments=()
    local verdict=0
    local exit=0

    if [ "$use_test_host" = "yes" ]; then
        arguments=(exec "tests/$name/bin/$configuration/net10.0-windows/$name.dll")
    else
        arguments=(test "tests/$name/$name.csproj" -c "$configuration" --no-build)
    fi

    if [ "${#filter_args[@]}" -gt 0 ]; then
        if [ "$use_test_host" = "yes" ]; then
            arguments+=("${filter_args[@]}")
        else
            arguments+=("--" "${filter_args[@]}")
        fi
    fi

    run dotnet "${arguments[@]}" >"$log_root/$name.log" 2>&1 || exit=$?

    if [ "$exit" -eq 0 ]; then
        verdict=0
    elif [ "$exit" -eq 5 ] || [ "$exit" -eq 8 ]; then
        verdict=2
    else
        verdict=1
    fi

    printf '%s\t%s\n' "$name" "$verdict" >>"$status_file"
}

export -f run_project_reported

if [ "$parallel" -gt 1 ]; then
    info "Параллельно: $parallel. Логи: $log_root"

    printf '%s\n' $targets |
        xargs -P "$parallel" -I{} bash -c 'run_project_reported "$@"' _ {} \
            "$configuration" "$use_test_host" "$log_root" "$status_file" "${filter_args[@]}" ||
        fail "Параллельный прогон оборвался. Логи: $log_root"

    failed=""
    empty=""

    while read -r name verdict; do
        case "$verdict" in
            0) ok "$name — зелёные" ;;
            2)
                note "$name — тесты не нашлись, лог: $log_root/$name.log"
                empty="$empty $name"
                ;;
            *)
                printf '    x %s — КРАСНЫЕ, лог: %s/%s.log\n' "$name" "$log_root" "$name" >&2
                failed="$failed $name"
                ;;
        esac
    done <"$status_file"
else
    failed=""
    empty=""
    count=0

    for name in $targets; do
        count=$((count + 1))
        verdict=0

        run_project "$name" || verdict=$?

        if [ "$verdict" -eq 1 ]; then
            failed="$failed $name"
        elif [ "$verdict" -eq 2 ]; then
            empty="$empty $name"
        fi
    done
fi

rm -f "$status_file"

step "Итог"

if [ -n "$empty" ]; then
    note "Тесты не нашлись в:$empty"
fi

if [ -n "$failed" ]; then
    printf '    x Красные проекты:%s\n' "$failed" >&2
    exit 1
fi

if [ -n "$empty" ]; then
    exit 1
fi

count=0
for name in $targets; do
    count=$((count + 1))
done

ok "Все проекты зелёные: $count."
