#!/usr/bin/env bash
#
# Форматирование и проверка форматирования CSharpier.
#
# CSharpier — единственный форматтер репозитория, .csharpierrc в нём нет, то
# есть действуют умолчания (printWidth 100, 4 пробела). Он лежит в локальном
# манифесте .config/dotnet-tools.json, а не в системном наборе, поэтому перед
# запуском скрипт зовёт dotnet tool restore — на свежей клоне `dotnet csharpier`
# просто не находится.
#
# CLI 1.x — с подкомандами. `format <path>` пишет, `check <path>` только
# проверяет. Вызов `dotnet csharpier <path>` без подкоманды в 1.x не
# существует (в 0.30.6 было наоборот).
#
# Версию важно не понижать: на 0.30.6 C# 14 extension members давали «Failed to
# compile so was not formatted» с кодом 1, и автоформат в CI ронялся целиком.
#
# Проекты в tests/ помечены CSharpier_Bypass: их файлы форматтер не трогает, и
# проверка на них ничего не требует.
#
# Примеры:
#   ./scripts/unix/format.sh                      отформатировать репозиторий
#   ./scripts/unix/format.sh --action check       проверить, ничего не меняя
#   ./scripts/unix/format.sh --path src/MARS.Gateway
#
# Опции:
#   --action format|check   что делать (по умолчанию format)
#   --path ПУТЬ             что обрабатывать (по умолчанию .)
#   --help                  эта справка

set -euo pipefail

MARS_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=common.sh
. "$MARS_SCRIPT_DIR/common.sh"

action="format"
path="."

while [ $# -gt 0 ]; do
    case "$1" in
        --action) action="$2"; shift 2 ;;
        --path) path="$2"; shift 2 ;;
        --help) mars_help "$0"; exit 0 ;;
        *) fail "Неизвестная опция: $1" ;;
    esac
done

cd "$(mars_root_or_die "$MARS_SCRIPT_DIR")"

need_cmd dotnet "Нужен .NET SDK версии из global.json (10.0.x)."

dotnet_tool_restore

step "CSharpier $action $path"

run_or_fail dotnet "CSharpier $action" csharpier "$action" "$path"

if [ "$action" = "check" ]; then
    ok "Форматирование соответствует."
else
    ok "Отформатировано. Изменения видны в git status — их нужно закоммитить в ту же задачу."
fi
