#!/usr/bin/env python3
"""Пересобирает профиль запуска MARS.Local в src/*/Properties/launchSettings.json.

Единственная реализация генератора: и scripts/windows/local.ps1, и
scripts/unix/local.sh зовут этот файл. Две независимые реализации одного и того
же расходились бы молча — ровно так, как это случилось бы с копией фильтров
покрытия, поэтому обёртки платформ ничего не генерируют сами.

Профиль описывает сервис, запущенный НА ХОСТЕ из IDE (F5), при инфраструктуре в
Docker. Без него локальный запуск не работает: в appsettings.json описан стенд
контейнеров (RabbitMq:Host = rabbitmq), а контейнерные имена с хоста не
резолвятся, и сервис падает на подключении к брокеру.

Значения берутся из scripts/local-launch.conf: общий блок плюс блок сервиса.
Подстановки ${ИМЯ} разрешаются по ШАБЛОНУ окружения (.env.development.example), а
не по файлу с секретами: иначе настоящий токен уехал бы в коммит. Ключи из строк
`#secrets` в профиль не попадают вовсе — их значения кладут в user-secrets.

Формат JSON пишется текстом, а не сериализатором: иначе переписался бы формат
всех профилей файла (чужой профиль заводит IDE), и diff правки выглядел бы как
переформатирование файла целиком. Переписывается один блок MARS.Local, чужие
профили сохраняются как есть.
"""

import argparse
import os
import re
import sys

PROFILE_NAME = "MARS.Local"
DATA_FILE = os.path.join("scripts", "local-launch.conf")
ENVIRONMENT_TEMPLATE = ".env.development.example"
LEGACY_ENVIRONMENT_TEMPLATE = ".env.example"


def fail(message):
    """Печатает ошибку и завершает работу кодом 1."""
    sys.stderr.write("    x %s\n" % message)
    sys.exit(1)


def read_variable_file(root, name):
    """Ключи файла окружения как словарь `ИМЯ -> значение` в порядке строк."""
    path = os.path.join(root, name)

    if not os.path.isfile(path):
        fail("Нет файла %s — из него берутся порты и dev-значения профиля." % name)

    values = {}

    with open(path, "r", encoding="utf-8") as handle:
        for raw in handle:
            line = raw.strip()

            if not line or line.startswith("#"):
                continue

            separator = line.find("=")

            if separator < 1:
                continue

            values[line[:separator].strip()] = line[separator + 1 :].strip()

    if not values:
        fail("В %s не нашлось ни одной переменной." % name)

    return values


def read_launch_data(root):
    """Читает scripts/local-launch.conf: общий блок, блоки сервисов, `#secrets`."""
    path = os.path.join(root, DATA_FILE)

    if not os.path.isfile(path):
        fail("Нет файла данных профиля: %s" % DATA_FILE)

    common = {}
    services = {}
    secrets = {}
    orphan = []
    current = None

    with open(path, "r", encoding="utf-8") as handle:
        for raw in handle:
            line = raw.strip()

            if not line:
                continue

            if line.startswith("#"):
                # `#secrets Ключ …` — напоминание, какие значения в профиль не попали.
                if line.startswith("#secrets "):
                    keys = line[len("#secrets ") :].strip()

                    if current is None:
                        orphan.append(keys)
                    else:
                        secrets[current] = keys

                continue

            if line.startswith("[") and line.endswith("]"):
                name = line[1:-1].strip()
                current = None if name == "common" else name

                if current is not None:
                    services.setdefault(current, {})

                continue

            separator = line.find("=")

            if separator < 1:
                fail("Строка '%s' в %s не вида 'ключ=значение'." % (line, DATA_FILE))

            key = line[:separator].strip()
            value = line[separator + 1 :].strip()

            if current is None:
                common[key] = value
            else:
                services[current][key] = value

    if orphan:
        fail(
            "Строка `#secrets` до первого блока [Сервис] в %s: %s."
            % (DATA_FILE, ", ".join(orphan))
        )

    return common, services, secrets


def expand(value, service, environment, template):
    """Подставляет ${ИМЯ} из шаблона окружения и падает на неизвестном имени.

    Молча оставленный ${ИМЯ} уехал бы в launchSettings.json строкой, и сервис
    прочитал бы из адреса буквально `${MARS_PORT_TTS}` — то есть упал бы не на
    подстановке, а на подключении, и искать пришлось бы в другом месте.
    """
    result = value
    missing = []

    for match in re.finditer(r"\$\{([A-Za-z0-9_]+)\}", value):
        name = match.group(1)

        if name not in environment:
            missing.append(name)
            continue

        result = result.replace(match.group(0), environment[name])

    if missing:
        fail(
            "%s, сервис %s: в %s нет переменных %s."
            % (DATA_FILE, service, template, ", ".join(sorted(set(missing))))
        )

    return result


def json_string(value):
    """Значение как JSON-строка: экранированы `\\` и `"`."""
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def profile_lines(service, common, services, secrets, environment, template):
    """Строки блока MARS.Local для одного сервиса."""
    if service not in services:
        fail(
            "В %s нет блока [%s] — локальный запуск описан не для всех сервисов."
            % (DATA_FILE, service)
        )

    entries = dict(common)
    port = ""

    for key, value in services[service].items():
        if key == "port":
            port = value
            continue

        entries[key] = value

    if not port.strip():
        fail(
            "В %s у блока [%s] нет строки port — applicationUrl собрать не из чего."
            % (DATA_FILE, service)
        )

    resolved_port = expand(port, service, environment, template)

    if not resolved_port.isdigit():
        fail(
            "В %s у блока [%s] порт '%s' не число." % (DATA_FILE, service, resolved_port)
        )

    lines = [
        "    %s: {" % json_string(PROFILE_NAME),
        '      "commandName": "Project",',
        # launchBrowser выключен: профиль поднимают ради API и оверлеев, а
        # вкладка браузера на каждый F5 только мешает.
        '      "launchBrowser": false,',
        '      "environmentVariables": {',
    ]

    names = list(entries.keys())

    for index, key in enumerate(names):
        value = expand(entries[key], service, environment, template)
        comma = "," if index < len(names) - 1 else ""
        lines.append(
            "        %s: %s%s" % (json_string(key), json_string(value), comma)
        )

    lines.append("      },")
    lines.append('      "applicationUrl": "http://localhost:%s"' % resolved_port)
    lines.append("    }")

    return lines


def foreign_profile_blocks(lines):
    """Блоки чужих профилей файла launchSettings.json, как они есть.

    Разбор построчный, а не по скобкам: имя профиля лежит на четырёх пробелах,
    а вложенные объекты — на шести и глубже, поэтому конец профиля однозначен.
    Так профили, заведённые IDE, сохраняются без изменений.
    """
    blocks = []
    current = None

    for line in lines:
        if current is None:
            if re.match(r'^\s{4}"', line):
                current = [line]
            else:
                continue
        else:
            current.append(line)

        if re.match(r"^\s{4}\}[,]?\s*$", current[-1]):
            blocks.append(current)
            current = None

    marker = "%s:" % json_string(PROFILE_NAME)

    return [block for block in blocks if not block[0].strip().startswith(marker)]


def build(lines, mars_local, newline):
    """Полный текст launchSettings.json с переписанным профилем MARS.Local."""
    profiles_start = -1

    for index, line in enumerate(lines):
        if re.match(r'^\s*"profiles"\s*:\s*\{\s*$', line):
            profiles_start = index
            break

    header = list(lines[: profiles_start + 1]) if profiles_start >= 0 else ["{", '  "profiles": {']

    blocks = [mars_local] + foreign_profile_blocks(lines)
    result = list(header)

    for block_index, block in enumerate(blocks):
        for line_index, line in enumerate(block):
            if line_index == len(block) - 1:
                # Запятую расставляет генератор: чужая запятая после последнего
                # профиля, если он единственный, сделала бы файл невалидным.
                line = re.sub(r",\s*$", "", line)

                if block_index < len(blocks) - 1:
                    line = line + ","

            result.append(line)

    result.append("  }")
    result.append("}")

    return newline.join(result) + newline


def main():
    parser = argparse.ArgumentParser(add_help=True, description=PROFILE_NAME)
    parser.add_argument("--root", default=".", help="корень репозитория")
    parser.add_argument(
        "--check",
        action="store_true",
        help="только проверить: код 1 при расхождении профиля с данными",
    )
    arguments = parser.parse_args()

    root = os.path.abspath(arguments.root)
    template = ENVIRONMENT_TEMPLATE

    print()
    print("==> Профиль %s: сервисы, запускаемые на хосте" % PROFILE_NAME)

    if not os.path.isfile(os.path.join(root, template)):
        print("    ! Нет %s — беру %s." % (ENVIRONMENT_TEMPLATE, LEGACY_ENVIRONMENT_TEMPLATE))
        template = LEGACY_ENVIRONMENT_TEMPLATE

    environment = read_variable_file(root, template)
    common, services, secrets = read_launch_data(root)

    sources = os.path.join(root, "src")
    targets = sorted(
        name
        for name in os.listdir(sources)
        if os.path.isfile(os.path.join(sources, name, "Properties", "launchSettings.json"))
    )

    if not targets:
        fail("В src/ не найдено ни одного Properties/launchSettings.json.")

    changed = 0
    reminders = []

    for service in targets:
        path = os.path.join(sources, service, "Properties", "launchSettings.json")

        with open(path, "r", encoding="utf-8", newline="") as handle:
            text = handle.read()

        newline = "\r\n" if "\r\n" in text else "\n"
        lines = re.split(r"\r?\n", text)

        if lines and lines[-1] == "":
            lines = lines[:-1]

        expected = build(
            lines,
            profile_lines(service, common, services, secrets, environment, template),
            newline,
        )

        if re.sub(r"\r?\n", "\n", expected) == re.sub(r"\r?\n", "\n", text):
            continue

        changed += 1

        if arguments.check:
            print("    ! %s: профиль разошёлся с %s." % (service, DATA_FILE))
            continue

        with open(path, "w", encoding="utf-8", newline="") as handle:
            handle.write(expected)

        print("    %s: профиль перезаписан." % service)

        if service in secrets:
            reminders.append("%s: %s" % (service, secrets[service]))

    orphans = sorted(name for name in services if name not in targets)

    if orphans:
        fail(
            "В %s описаны сервисы, которых нет в src/: %s."
            " Удалите блок или добавьте сервис — иначе описание молча устаревает."
            % (DATA_FILE, ", ".join(orphans))
        )

    if arguments.check:
        if changed:
            fail(
                "Профиль %s разошёлся у сервисов: %d."
                " Запустите скрипт без проверки." % (PROFILE_NAME, changed)
            )

        print("    Профиль %s соответствует %s у всех сервисов." % (PROFILE_NAME, DATA_FILE))
    elif changed == 0:
        print("    Профиль %s уже соответствовал %s." % (PROFILE_NAME, DATA_FILE))
    else:
        print("    Профиль %s пересобран у сервисов: %d." % (PROFILE_NAME, changed))

    if not arguments.check and reminders:
        print("    ! Значений в профиле нет: они секретные или задаются владельцем.")
        print("    ! Положите их в user-secrets, например:")
        print('    dotnet user-secrets set "Twitch:Secret" "…" --project src/MARS.TwitchCore')

        for reminder in reminders:
            print("    %s" % reminder)


if __name__ == "__main__":
    main()
