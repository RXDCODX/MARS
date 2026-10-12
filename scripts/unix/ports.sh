#!/usr/bin/env bash
#
# Пересобирает производные хостовые порты стенда из MARS_PORT_BASE.
#
# Хостовые порты задаёт одна переменная на среду — MARS_PORT_BASE, а сами
# значения лежат в явных MARS_PORT_<СЕРВИС>. Арифметики compose не умеет: ни
# ${MARS_PORT_BASE + 17}, ни `base + номер` он не считает, поэтому сумму
# приходится записывать числом. Скрипт делает это за человека: берёт
# MARS_PORT_BASE из файла и переписывает строки MARS_PORT_<СЕРВИС> в порядке их
# следования — base, base + 1, base + 2 и так далее.
#
# Порядок строк в блоке и есть нумерация, и это не деталь оформления: сдвинуть
# стенд на другую базу значит поменять одну строку MARS_PORT_BASE и запустить этот
# скрипт. Номер каждой переменной охраняет тест
# PortAllocationTests.КаждыйПортРавенБазовомуПлюсНомер, а скрипт только заполняет
# файл по нему: расходиться с тестом он не может, потому что схему не дублирует,
# а читает порядок строк из того же файла.
#
# По умолчанию обрабатываются оба шаблона сред и .env.example. Реальный файл
# окружения (.env.development, .env.production) в список не входит: он в git не
# лежит, и правка чужого файла с секретами не должна быть побочным эффектом
# запуска без аргументов — его передают в --path явно, когда блок портов в нём уже
# есть.
#
# Файл без строки MARS_PORT_BASE не дополняется: схема должна быть в нём целиком,
# и склеивать её из ничего значило бы расставить номера наугад. Такой файл берут из
# шаблона или переносят в него блок портов.
#
# Примеры:
#   ./scripts/unix/ports.sh
#   ./scripts/unix/ports.sh --path .env.development --base 6101
#   ./scripts/unix/ports.sh --check
#
# Опции:
#   --path ФАЙЛ         файл для обработки; можно указать несколько раз
#   --base N            новое значение MARS_PORT_BASE
#   --check             только проверить: код возврата 1 при расхождении
#   --help              эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

paths=()
base=""
check="no"

while [ $# -gt 0 ]; do
    case "$1" in
        --path) paths+=("$2"); shift 2 ;;
        --base) base="$2"; shift 2 ;;
        --check) check="yes"; shift ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

if [ "${#paths[@]}" -eq 0 ]; then
    paths=('.env.production.example' '.env.development.example' '.env.example')
fi

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

# update_port_file переписывает порты одного файла.
#
# Возвращает 1 только в режиме --check и только при расхождении: тогда вызывающий
# считает такие файлы, чтобы завершиться с ошибкой один раз, а не на первом.
# Перевод строки переносится как есть (файлы в репозитории CRLF): перезапись
# сменила бы окончания строк во всём файле, и diff показал бы его целиком.

update_port_file() {
    local file="$1"
    local base="$2"
    local check="$3"
    local found
    local tmp

    if [ ! -f "$file" ]; then
        fail "Нет файла $file."
    fi

    if [ -z "$base" ]; then
        found="$(env_value "$file" MARS_PORT_BASE)"

        if [ -z "$found" ]; then
            fail "$file не содержит MARS_PORT_BASE. Задайте базу через --base или возьмите блок портов из шаблона."
        fi

        base="$found"
    fi

    case "$base" in
        ''|*[!0-9]*) fail "$file: MARS_PORT_BASE=$base — не целое число." ;;
    esac

    # Последняя строка .env.production.example идёт без завершающего перевода
    # строки, и перезапись не имеет права его добавить: иначе diff показал бы
    # лишнюю строку в файле, которого схема портов не касалась вовсе.
    if [ -n "$(tail -c 1 "$file" | tr -d '\n')" ]; then
        trailing="no"
    else
        trailing="yes"
    fi

    tmp="$(mktemp)"

    awk -v base="$base" -v trailing="$trailing" '
        BEGIN { n = 0; seen = 0 }
        {
            cr = ""
            line = $0

            if (line ~ /\r$/) {
                cr = "\r"
                sub(/\r+$/, "", line)
            }

            if (line ~ /^MARS_PORT_BASE=/) {
                line = "MARS_PORT_BASE=" base
            } else if (line ~ /^MARS_PORT_[A-Z0-9_]+=/) {
                eq = index(line, "=")
                line = substr(line, 1, eq) (base + n)
                n++
            }

            if (seen > 0) {
                printf "\n"
            }

            printf "%s%s", line, cr
            seen++
        }
        END {
            if (trailing == "yes" && seen > 0) {
                printf "\n"
            }
        }
    ' "$file" >"$tmp"

    if cmp -s "$file" "$tmp"; then
        rm -f "$tmp"
        ok "$file уже соответствует MARS_PORT_BASE=$base."
        return 0
    fi

    if [ "$check" = "yes" ]; then
        diff -u "$file" "$tmp" || true
        rm -f "$tmp"
        note "$file: расхождений с MARS_PORT_BASE=$base."
        return 1
    fi

    mv "$tmp" "$file"
    ok "$file: порты пересобраны от MARS_PORT_BASE=$base."
    return 0
}

step "Порты: база $(if [ -n "$base" ]; then printf '%s' "$base"; else printf 'из файла'; fi)"

changed=0

for file in "${paths[@]}"; do
    if ! update_port_file "$file" "$base" "$check"; then
        changed=$((changed + 1))
    fi
done

if [ "$check" = "yes" ]; then
    if [ "$changed" -gt 0 ]; then
        fail "Порты разошлись с MARS_PORT_BASE: файлов $changed. Запустите скрипт без --check, чтобы пересобрать."
    fi

    ok "Все проверенные файлы соответствуют схеме."
fi
