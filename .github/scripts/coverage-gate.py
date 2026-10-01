#!/usr/bin/env python3
"""Порог покрытия кода для CI.

Coverlet не умеет надёжно проверять порог (в его документации флаг
--coverlet-threshold есть, а в Known Limitations той же версии написано, что
проверка не реализована), поэтому порог считаем сами. Считаем именно покрытие
МЕТОДОВ: в формате Cobertura их видно только в узлах <methods>, а line-rate и
branch-rate в <coverage> методов не содержат.

Два числа на выходе:

* порог проверяется по СЛИТОМУ отчёту ReportGenerator (--merged). Суммировать
  отчёты тестовых проектов нельзя: MARS.Shared инструментируется в каждом из
  них и посчитал быcя 16 раз. ReportGenerator сливает покрытие построчно, то
  есть объединением покрытых строк, и это единственный корректный источник
  итоговой цифры;
* таблица по проектам строится по индивидуальным отчётам (--reports-dir) —
  она нужна, чтобы видеть, до какого порно дотягивать конкретный сервис.

Метод считается покрытым, если хотя бы одна его строка имеет hits > 0. Методы
без строк (абстрактные, внешние) не инструментируются и в знаменатель не идут,
но их количество выводится отдельной строкой, чтобы цифра не выглядела
лучше, чем она есть.

Пустой набор отчётов — ошибка, а не ноль процентов: иначе сломанная выгрузка
артефакта дала бы зелёный статус при нулевом покрытии.

Использование:
    python3 .github/scripts/coverage-gate.py \
        --merged coverage-report/Cobertura.xml \
        --reports-dir reports --reports-pattern '*.cobertura*.xml' \
        --threshold-methods 95 --summary-file "$GITHUB_STEP_SUMMARY"
"""

from __future__ import annotations

import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET


def _local(tag: str) -> str:
    """Обрезает XML-namespace: Cobertura его не использует, но полезно терпеть."""
    return tag.rpartition("}")[2]


def _number(element: ET.Element | None, attribute: str) -> int | None:
    if element is None:
        return None
    value = element.get(attribute)
    if value is None:
        return None
    try:
        return int(float(value))
    except ValueError:
        return None


class Stats:
    __slots__ = ("methods", "covered_methods", "skipped_methods", "lines", "covered_lines",
                 "branches", "covered_branches")

    def __init__(self) -> None:
        self.methods = 0
        self.covered_methods = 0
        self.skipped_methods = 0
        self.lines: int | None = None
        self.covered_lines: int | None = None
        self.branches: int | None = None
        self.covered_branches: int | None = None

    def add(self, other: "Stats") -> None:
        self.methods += other.methods
        self.covered_methods += other.covered_methods
        self.skipped_methods += other.skipped_methods
        for name in ("lines", "covered_lines", "branches", "covered_branches"):
            own = getattr(self, name)
            incoming = getattr(other, name)
            if incoming is None:
                continue
            setattr(self, name, incoming if own is None else own + incoming)

    @property
    def method_rate(self) -> float:
        return 100.0 * self.covered_methods / self.methods if self.methods else 0.0

    def rate(self, covered: int | None, total: int | None) -> str:
        if not total:
            return "—"
        return f"{100.0 * covered / total:.1f}%"


def parse_report(path: str) -> tuple[str, Stats]:
    """Разбирает один cobertura-отчёт. Возвращает (имя проекта, статистика)."""
    root = ET.parse(path).getroot()
    if _local(root.tag) != "coverage":
        raise ValueError(f"{path}: корневой узел <{_local(root.tag)}>, а не <coverage>")

    stats = Stats()
    stats.lines = _number(root, "lines-valid")
    stats.covered_lines = _number(root, "lines-covered")
    stats.branches = _number(root, "branches-valid")
    stats.covered_branches = _number(root, "branches-covered")

    for element in root.iter():
        if _local(element.tag) != "method":
            continue
        lines = [child for child in element if _local(child.tag) == "lines"]
        hits = [
            _number(line, "hits")
            for line in lines[0]
            if _local(line.tag) == "line"
        ] if lines else []
        hits = [value for value in hits if value is not None]
        if not hits:
            # Абстрактный или внешний метод: строк нет, покрытие не измеряется.
            stats.skipped_methods += 1
            continue
        stats.methods += 1
        if any(value > 0 for value in hits):
            stats.covered_methods += 1

    # Имя проекта — из префикса файла, который coverlet получает через
    # --coverlet-file-prefix: MARS.Shared.Tests.coverage.cobertura.<ts>.xml
    name = os.path.basename(path).split(".coverage")[0] or os.path.basename(path)
    return name, stats


def find_reports(directory: str, pattern: str) -> list[str]:
    return sorted(
        path
        for path in glob.glob(os.path.join(directory, "**", pattern), recursive=True)
        if os.path.isfile(path)
    )


def render_table(rows: list[tuple[str, Stats]]) -> str:
    lines = [
        "| Проект | Методов | Покрыто | % методов | Строк | Ветвей |",
        "|---|---:|---:|---:|---:|---:|",
    ]
    for name, stats in rows:
        lines.append(
            "| {name} | {methods} | {covered} | {rate} | {lines} | {branches} |".format(
                name=name,
                methods=stats.methods,
                covered=stats.covered_methods,
                rate=stats.rate(stats.covered_methods, stats.methods),
                lines=stats.rate(stats.covered_lines, stats.lines),
                branches=stats.rate(stats.covered_branches, stats.branches),
            )
        )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--merged", required=True, help="слитый cobertura-отчёт ReportGenerator")
    parser.add_argument("--reports-dir", default="", help="папка с отчётами по проектам")
    parser.add_argument("--reports-pattern", default="*.cobertura*.xml")
    parser.add_argument("--threshold-methods", type=float, required=True)
    parser.add_argument("--summary-file", default="", help="куда дописать markdown (GITHUB_STEP_SUMMARY)")
    args = parser.parse_args()

    if not os.path.isfile(args.merged):
        print(f"Слитый отчёт покрытия не найден: {args.merged}", file=sys.stderr)
        print("Это значит, что тесты не отдали cobertura, а не что покрытие равно нулю.",
              file=sys.stderr)
        return 2

    _, total = parse_report(args.merged)

    report_paths = find_reports(args.reports_dir, args.reports_pattern) if args.reports_dir else []
    rows = [parse_report(path) for path in report_paths]
    rows.sort(key=lambda row: (-row[1].method_rate, row[0]))

    summary = ["## Покрытие кода", ""]
    if rows:
        summary += [render_table(rows), ""]
    summary += [
        f"**Порог по методам: {args.threshold_methods:g}%**, фактически "
        f"**{total.method_rate:.1f}%** "
        f"({total.covered_methods} из {total.methods} методов).",
        "",
        f"Покрытие строк: {total.rate(total.covered_lines, total.lines)}, "
        f"ветвей: {total.rate(total.covered_branches, total.branches)}.",
        "",
    ]
    if total.skipped_methods:
        summary += [
            f"Вне знаменателя {total.skipped_methods} методов без строк "
            "(абстрактные, внешние, сгенерированные) — покрыть их нельзя.",
            "",
        ]

    text = "\n".join(summary)
    print(text)
    if args.summary_file:
        with open(args.summary_file, "a", encoding="utf-8") as handle:
            handle.write(text + "\n")

    if not total.methods:
        print("В слитом отчёте нет ни одного метода со строками.", file=sys.stderr)
        return 2
    if total.method_rate < args.threshold_methods:
        print(
            f"Покрытие методов {total.method_rate:.1f}% ниже порога "
            f"{args.threshold_methods:g}%.",
            file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())