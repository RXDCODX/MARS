# Общие помощники скриптов scripts/unix.
#
# Файл только подключается (source). `set -euo pipefail` здесь намеренно НЕ
# задаётся: каждый скрипт включает его сам, а common.sh, подключённый к
# существующей оболочке, не должен молча менять её настройки.
#
# Общее у всех скриптов одно: корень ищется вверх по дереву от каталога
# скрипта, а не берётся из текущего каталога. Скрипты зовут и из корня, и из
# Makefile, и из произвольного места, а путь, зашитый в скрипт, сломался бы
# при первом же клоне у другого человека.
#
# Требования к bash — 3.2 (то, что стоит в macOS по умолчанию): нет
# ассоциативных массивов, нет ${var,,}, нет mapfile и нет `wait -n`.
#
# Источник правды по составу репозитория — сам репозиторий: тестовые проекты
# берутся из каталога tests/, образы для публикации — из матрицы
# release-microservices.yml. Второй список в скрипте разошёлся бы с первым
# при первом же добавлении сервиса, и разошёлся бы молча.

# --- Цвета -----------------------------------------------------------------
# Цвета выключаются без терминала и по NO_COLOR: иначе escape-последовательности
# попадают в лог CI и в перенаправленный вывод.

if [ -t 1 ] && [ -z "${NO_COLOR:-}" ]; then
    C_RESET=$'\033[0m'
    C_STEP=$'\033[36m'
    C_INFO=$'\033[90m'
    C_OK=$'\033[32m'
    C_NOTE=$'\033[33m'
    C_FAIL=$'\033[31m'
else
    C_RESET=""
    C_STEP=""
    C_INFO=""
    C_OK=""
    C_NOTE=""
    C_FAIL=""
fi

step() {
    printf '\n%s==> %s%s\n' "$C_STEP" "$1" "$C_RESET"
}

info() {
    printf '    %s%s%s\n' "$C_INFO" "$1" "$C_RESET"
}

ok() {
    printf '    %s%s%s\n' "$C_OK" "$1" "$C_RESET"
}

note() {
    printf '    %s! %s%s\n' "$C_NOTE" "$1" "$C_RESET"
}

# fail печатает в stderr и завершает скрипт. Именно fail, а не `echo` + `return`:
# сообщение должно быть и в stdout конвейера, и видно в CI.

fail() {
    printf '%s    x %s%s\n' "$C_FAIL" "$1" "$C_RESET" >&2
    exit 1
}

# mars_help печатает шапку скрипта: комментарии от второй строки до `set -euo`.
# Справка не дублируется текстом отдельно, иначе она разошлась бы ровно там,
# где пришлось бы читать.

mars_help() {
    awk 'NR > 1 && /^set -euo/ { exit } NR > 1 { sub(/^# ?/, ""); print }' "$1"
}

# --- Корень репозитория ----------------------------------------------------

mars_find_root() {
    local dir="$1"

    while [ -n "$dir" ] && [ "$dir" != "/" ]; do
        if [ -f "$dir/MARS.slnx" ]; then
            printf '%s' "$dir"
            return 0
        fi

        dir="$(dirname "$dir")"
    done

    return 1
}

mars_root_or_die() {
    local root

    if ! root="$(mars_find_root "${1:-.}")"; then
        fail "Корень репозитория не найден вверх по дереву от ${1:-.}. Ожидался файл MARS.slnx."
    fi

    printf '%s' "$root"
}

# --- Внешние команды -------------------------------------------------------

need_cmd() {
    local name="$1"
    local hint="${2:-}"

    if command -v "$name" >/dev/null 2>&1; then
        return 0
    fi

    if [ -n "$hint" ]; then
        fail "Команда '$name' не найдена в PATH. $hint"
    fi

    fail "Команда '$name' не найдена в PATH."
}

# run печатает вывод программы и возвращает её код. Отдельная функция нужна,
# чтобы не писать `|| true` в вызывающем коде: stderr программы при `set -e`
# иначе обрывает скрипт раньше, чем будет показан её код, — то есть ровно там,
# где помощь нужнее всего.

run() {
    local file="$1"
    shift

    set +e
    "$file" "$@" 2>&1
    local exit=$?
    set -e

    return "$exit"
}

# run_or_fail печатает вывод и завершает скрипт на ненулевом коде.

run_or_fail() {
    local file="$1"
    shift
    local description="$1"
    shift

    local exit=0
    run "$file" "$@" || exit=$?

    if [ "$exit" -ne 0 ]; then
        fail "$description не удалась (код $exit)."
    fi
}

# --- Состав репозитория ----------------------------------------------------

# test_projects печатает имена тестовых проектов.
#
# Источник правды — каталог tests/, а не матрица ci.yml: проект, забытый в
# матрице, выпал бы и из списка, и это было бы не видно.
#
# MARS.ClientUi.Tests исключается: это навигационные тесты клиента на
# Playwright, им нужен поднятый стенд и образ с браузерами. Локально они
# гоняются через e2e.sh, в матрицу tests в ci.yml они тоже не входят.

test_projects() {
    local directory name

    for directory in tests/*/; do
        name="$(basename "$directory")"

        case "$name" in
            *.Tests)
                if [ -f "tests/$name/$name.csproj" ] && [ "$name" != "MARS.ClientUi.Tests" ]; then
                    printf '%s\n' "$name"
                fi
                ;;
        esac
    done | sort
}

resolve_test_project() {
    local name="$1"
    local available

    available="$(test_projects)"

    if printf '%s\n' "$available" | grep -qx -- "$name"; then
        printf '%s' "$name"
        return 0
    fi

    # printf без кавычек: список имён нужен через пробел, а не как один аргумент.
    fail "Нет такого тестового проекта: $name. Доступны: $(printf '%s ' $available)(MARS.ClientUi.Tests гоняет e2e.sh — ему нужен стенд)."
}

# resolve_project_file ищет .csproj по имени каталога в src/ и tests/.
#
# Службы в src/ и тесты в tests/ называются одинаково (MARS.Gateway и
# MARS.Gateway.Tests), поэтому достаточно совпадения по имени каталога. Путь
# принимается и явно — на случай нестандартного расположения.

resolve_project_file() {
    local name="$1"

    case "$name" in
        *.csproj)
            if [ -f "$name" ]; then
                printf '%s' "$name"
                return 0
            fi

            fail "Файл проекта не найден: $name"
            ;;
    esac

    if [ -f "src/$name/$name.csproj" ]; then
        printf '%s' "src/$name/$name.csproj"
        return 0
    fi

    if [ -f "tests/$name/$name.csproj" ]; then
        printf '%s' "tests/$name/$name.csproj"
        return 0
    fi

    fail "Проект '$name' не найден. Ожидался src/$name/$name.csproj. Имя указывается без пути и без .csproj."
}

# release_services печатает матрицу публикации как `service<TAB>project<TAB>dockerfile`.
#
# Список намеренно не зашит в скрипт. Матрица workflow — единственное место,
# где решено, какие образы публикуются: `videos365` в compose есть, а в
# публикации его нет, и зашитый список однажды опубликовал бы лишний образ или,
# что хуже, промолчал бы про забытый.
#
# awk, а не grep: строка матрицы — это три поля подряд, и собирать их по
# одному ключу значило бы разбирать YAML.

release_services() {
    awk '
        /^[[:space:]]*-[[:space:]]*service:[[:space:]]*/ {
            if (service != "") {
                print service "\t" project "\t" dockerfile
            }
            service = $3
            project = ""
            dockerfile = "Dockerfile"
            next
        }
        /^[[:space:]]+project:[[:space:]]*/ && service != "" {
            project = $2
            next
        }
        /^[[:space:]]+dockerfile:[[:space:]]*/ && service != "" {
            dockerfile = $2
            next
        }
        END {
            if (service != "") {
                print service "\t" project "\t" dockerfile
            }
        }
    ' .github/workflows/release-microservices.yml
}

# show_release_services печатает матрицу человекочитаемо.

show_release_services() {
    local service project dockerfile

    release_services | while IFS=$'\t' read -r service project dockerfile; do
        [ -n "$project" ] || fail "Строка матрицы '$service' не объявляет project."

        printf '    %-16s %-24s %s\n' "$service" "$project" "$dockerfile"
    done
}

# select_release_services отбирает строки по именам сервисов. Пустой список
# означает «все сервисы»: перечислять 15 имён руками — источник опечаток.

select_release_services() {
    local wanted="$*"
    local all
    local service

    all="$(release_services)"

    if [ -z "$wanted" ]; then
        printf '%s\n' "$all"
        return 0
    fi

    for service in $wanted; do
        if ! printf '%s\n' "$all" | cut -f1 | grep -qx -- "$service"; then
            fail "В матрице публикации нет сервиса: $service. Доступны: $(printf '%s\n' "$all" | cut -f1 | tr '\n' ' ')"
        fi
    done

    printf '%s\n' "$all" | awk -F'\t' -v wanted="$wanted" '
        BEGIN { count = split(wanted, names, " ") }
        {
            for (i = 1; i <= count; i++) {
                if ($1 == names[i]) { print; break }
            }
        }
    '
}

# --- Инфраструктура --------------------------------------------------------

# compose_args печатает аргументы `docker compose --env-file … -f …`: прод-стенд
# или dev.
#
# Файл окружения выбирается тем же аргументом, что и compose-файл, и это не
# совпадение: в docker-compose.dev.yml стоит ASPNETCORE_ENVIRONMENT=Development
# (сервисы читают appsettings.Development.json), а в docker-compose.yml —
# Production. Секреты среды обязаны соответствовать той же среде, поэтому
# отдельного переключателя профиля здесь нет: разъехаться могут только два
# переключателя подряд, а их один.
#
# --env-file обязателен: compose сам читает дефолтный .env, которого в
# репозитории больше нет, молча взял бы значения по умолчанию из ${ПЕРЕМЕННАЯ:-}
# и поднял стенд на пустых паролях.
#
# docker-compose.dev.yml подключается только явно и вторым файлом: он назван не
# `override`, чтобы обычный `docker compose up` не подхватывал hot-reload
# (`dotnet watch`) молча.

compose_args() {
    if [ "${1:-}" = "dev" ]; then
        printf '%s' "--env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml"
    else
        printf '%s' "--env-file .env.production -f docker-compose.yml"
    fi
}

# ensure_env_file создаёт файл окружения (.env.development или .env.production) из
# его шаблона, если его ещё нет.
#
# Файлы окружений в git не попадают, а compose получает нужный флагом
# --env-file. Без этого шага стенд поднимается с пустыми паролями, а
# 01-databases.sh останавливается на первой проверке — и падает не с «нет
# пароля», а с невнятным сообщением Postgres.
#
# Существующий файл не перезаписывается: в нём настоящие секреты стенда, а
# шаблон содержит пустые значения. Шаблон копируется только в отсутствие файла, и
# то по шаблону той среды, которую запускают.
#
# Откат на .env.example — не украшение, а страховка на время перехода: пока
# шаблонов окружений в репозитории нет, единственный источник значений для стенда
# это .env.example, и падать из-за его отсутствия было бы хуже, чем создать файл
# из него с предупреждением.

ensure_env_file() {
    environment=".env.production"

    if [ "${1:-}" = "dev" ]; then
        environment=".env.development"
    fi

    if [ -f "$environment" ]; then
        info "$environment уже есть — не трогаю."
        return 0
    fi

    if [ ! -f "$environment.example" ]; then
        if [ ! -f .env.example ]; then
            fail "Нет шаблона $environment.example и .env.example, из которого можно создать $environment."
        fi

        cp .env.example "$environment"
        note "$environment создан из .env.example: шаблонов окружений пока нет в репозитории."
        note "Значения в нём стендовые. Для боевого стенда секреты заполняются вручную."
        return 0
    fi

    cp "$environment.example" "$environment"
    ok "Создан $environment из шаблона. Значения в нём — стендовые, секреты в нём пустые."
}

# dotnet_tool_restore восстанавливает локальные инструменты репозитория.
#
# reportgenerator и csharpier лежат в .config/dotnet-tools.json, а не в
# системном наборе. На свежей клоне `dotnet csharpier` не найдётся, и скрипт
# падал бы на «инструмент не установлен» вместо сути дела.

dotnet_tool_restore() {
    run_or_fail dotnet "Восстановление локальных инструментов (dotnet tool restore)" \
        tool restore
}

# python_cmd печатает имя команды Python: python3 в linux, python в venv.

python_cmd() {
    if command -v python3 >/dev/null 2>&1; then
        printf 'python3'
        return 0
    fi

    if command -v python >/dev/null 2>&1; then
        printf 'python'
        return 0
    fi

    fail "Не найден ни python3, ни python. Ими запускаются .github/scripts/coverage-gate.py и coverage-gaps.py — гейт покрытия без них не проверяется."
}

# git_owner печатает владельца репозитория из origin для имени образа
# ghcr.io/<owner>/mars-<service>.
#
# Поддерживаются обе формы: https://github.com/<owner>/<repo> и
# git@github.com:<owner>/<repo>.git. Регистр владельца в GHCR не важен —
# сам workflow приводит его к нижнему.

git_owner() {
    local url

    url="$(git remote get-url origin 2>/dev/null || true)"

    if [ -z "$url" ]; then
        return 0
    fi

    printf '%s' "$url" | sed -e 's,.*[:/],,' -e 's,\.git$,,' | tr '[:upper:]' '[:lower:]'
}

# --- Поиск по файлам -------------------------------------------------------

# Каталоги, которые не обходятся никогда: там копии исходников и артефакты, а
# совпадение в них ничего не значит. Исключение задаётся через prune, а не
# пост-фильтром: обход node_modules и .git дороже самой проверки.
#
# Список разделён пробелами и разворачивается в предикаты по одному на имя:
# после `-o` find ждёт новый предикат, а не голое слово, и запись вида
# `-name .git -o obj` заканчивается ошибкой «paths must precede expression».
MARS_SKIP_DIR='.git .opencode .mimocode .vs .vscode .idea .yarn obj bin node_modules TestResults coverage-local coverage-report graphify-out dist ui-dist secrets publish'

# search_files печатает подходящие файлы относительно $1 (корень поиска) по
# списку шаблонов $2 (`*.cs`, `Dockerfile`).

search_files() {
    local root="$1"
    shift

    local expression=()
    local skip=()
    local pattern

    for pattern in $MARS_SKIP_DIR; do
        skip+=(-name "$pattern" -o)
    done

    unset "skip[$((${#skip[@]} - 1))]"

    for pattern in "$@"; do
        expression+=(-name "$pattern" -o)
    done

    # Хвост «-o» снимается: после него условие кончается, и find ругается.
    unset "expression[$((${#expression[@]} - 1))]"

    # -type d перед -prune обязателен: иначе prune применялся бы к файлам.
    # Символические ссылки find по умолчанию не разворачивает, поэтому цикла
    # по ним не возникает.
    find "$root" \
        -type d \( "${skip[@]}" \) -prune -o \
        -type f \( "${expression[@]}" \) -print 2>/dev/null
}
