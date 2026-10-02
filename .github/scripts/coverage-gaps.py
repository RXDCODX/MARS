#!/usr/bin/env python3
"""Список самых непокрытых мест по слитому отчёту покрытия.

coverage-gate.py отвечает на вопрос «проходит ли порог», но не отвечает на
вопрос «что именно писать в тесты». Этот скрипт разбирает тот же слитый
Cobertura.xml и ранжирует классы по числу непокрытых методов.

Правило «метод покрыт, если хотя бы одна его строка имеет hits > 0» НЕ
дублируется, а импортируется из coverage-gate.py: две реализации одного
правила в двух файлах рано или поздно разойдутся, и цифры перестанут
сравниваться.

Использование:
    python3 .github/scripts/coverage-gaps.py \\
        --merged coverage-local/coverage-report/Cobertura.xml --top 40
    python3 .github/scripts/coverage-gaps.py --merged ... --package MARS.OBS --methods
    python3 .github/scripts/coverage-gaps.py --merged ... --format json --output gaps.json
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import os
import sys
import xml.etree.ElementTree as ET

_GATE_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "coverage-gate.py")


def _load_gate():
    """Загружает coverage-gate.py как модуль: имя файла с дефисом не импортируется."""
    spec = importlib.util.spec_from_file_location("coverage_gate", _GATE_PATH)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Не удалось загрузить {_GATE_PATH}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


_gate = _load_gate()
_local = _gate._local
_number = _gate._number


class ClassStats:
    __slots__ = ("name", "filename", "methods", "covered_methods", "uncovered", "lines", "covered_lines")

    def __init__(self, name: str, filename: str) -> None:
        self.name = name
        self.filename = filename
        self.methods = 0
        self.covered_methods = 0
        self.uncovered: list[str] = []
        self.lines = 0
        self.covered_lines = 0

    @property
    def uncovered_count(self) -> int:
        return self.methods - self.covered_methods

    @property
    def method_rate(self) -> float:
        return 100.0 * self.covered_methods / self.methods if self.methods else 100.0


def read_classes(path: str) -> list[ClassStats]:
    root = ET.parse(path).getroot()
    if _local(root.tag) != "coverage":
        raise ValueError(f"{path}: корневой узел <{_local(root.tag)}>, а не <coverage>")

    classes: list[ClassStats] = []
    for element in root.iter():
        if _local(element.tag) != "class":
            continue
        stats = ClassStats(element.get("name") or "?", element.get("filename") or "?")

        for child in element:
            tag = _local(child.tag)
            if tag == "lines":
                for line in child:
                    if _local(line.tag) != "line":
                        continue
                    stats.lines += 1
                    hits = _number(line, "hits")
                    if hits:
                        stats.covered_lines += 1
            elif tag == "methods":
                for method in child:
                    if _local(method.tag) != "method":
                        continue
                    method_lines = [
                        sub for sub in method if _local(sub.tag) == "lines"
                    ]
                    hits = [
                        _number(line, "hits")
                        for line in (method_lines[0] if method_lines else [])
                        if _local(line.tag) == "line"
                    ]
                    hits = [value for value in hits if value is not None]
                    if not hits:
                        # Без строк — метод не инструментируется, в знаменатель
                        # не идёт (то же правило, что и в гейте).
                        continue
                    stats.methods += 1
                    if any(value > 0 for value in hits):
                        stats.covered_methods += 1
                    else:
                        stats.uncovered.append(
                            f"{method.get('name') or '?'}{method.get('signature') or ''}"
                        )

        classes.append(stats)
    return classes


def _package_of(class_name: str, filename: str = "") -> str:
    """Имя сервиса, которому принадлежит класс.

    По классу определить нельзя: у всех классов префикс MARS, а различает их
    только второй сегмент. Надёжнее путь файла — там видно проект буквально.
    """
    path = filename.replace("\\", "/")
    if "/src/" in path:
        return path.split("/src/", 1)[1].split("/")[0]
    parts = class_name.split(".")
    return ".".join(parts[:2]) if len(parts) > 1 else class_name


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--merged", required=True, help="слитый cobertura-отчёт ReportGenerator")
    parser.add_argument("--top", type=int, default=30, help="сколько классов показать (0 — все)")
    parser.add_argument(
        "--min-methods",
        type=int,
        default=2,
        help="классы с меньшим числом методов скрывать: один непокрытый сеттер ничего не решает",
    )
    parser.add_argument("--package", default="", help="фильтр по имени пакета/сервиса, например MARS.OBS")
    parser.add_argument(
        "--fully-uncovered-only",
        action="store_true",
        help="только классы, у которых не покрыт ни один метод",
    )
    parser.add_argument(
        "--methods",
        action="store_true",
        help="под каждым классом перечислить непокрытые методы",
    )
    parser.add_argument("--format", choices=("markdown", "json"), default="markdown")
    parser.add_argument("--output", default="", help="куда писать вывод (иначе stdout)")
    args = parser.parse_args()

    if not os.path.isfile(args.merged):
        print(f"Отчёт покрытия не найден: {args.merged}", file=sys.stderr)
        return 2

    classes = read_classes(args.merged)
    selected = [item for item in classes if item.uncovered_count > 0]
    if args.package:
        needle = args.package.lower()
        selected = [item for item in selected if needle in _package_of(item.name, item.filename).lower()]
    if args.min_methods:
        selected = [item for item in selected if item.methods >= args.min_methods]
    if args.fully_uncovered_only:
        selected = [item for item in selected if item.covered_methods == 0]
    selected.sort(key=lambda item: (-item.uncovered_count, item.name))
    total_uncovered = sum(item.uncovered_count for item in selected)
    if args.top:
        selected = selected[: args.top]

    if args.format == "json":
        payload = [
            {
                "class": item.name,
                "file": item.filename,
                "methods": item.methods,
                "covered": item.covered_methods,
                "uncovered": item.uncovered_count,
                "method_rate": round(item.method_rate, 1),
                "uncovered_methods": item.uncovered,
            }
            for item in selected
        ]
        text = json.dumps(payload, ensure_ascii=False, indent=2)
    else:
        lines = [
            "| Класс | Методов | Не покрыто | % методов | Файл |",
            "|---|---:|---:|---:|---|",
        ]
        for item in selected:
            lines.append(
                f"| `{item.name}` | {item.methods} | {item.uncovered_count} | "
                f"{item.method_rate:.1f}% | `{item.filename}` |"
            )
            if args.methods:
                for method in item.uncovered:
                    lines.append(f"| ↳ {method} | | | | |")
        text = "\n".join(lines)
        text += (
            f"\n\nНепокрытых методов в выборке: {total_uncovered}"
            f"{' (показаны первые ' + str(args.top) + ')' if args.top and total_uncovered > len(selected) else ''}."
        )

    if args.output:
        with open(args.output, "w", encoding="utf-8") as handle:
            handle.write(text + "\n")
        print(f"Записано: {args.output}")
    else:
        print(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
