#!/usr/bin/env python3
"""Create a verbose GitHub Actions summary from a Stryker.NET JSON report."""

from __future__ import annotations

import argparse
import json
import os
import sys
from collections import Counter
from pathlib import Path
from typing import Any


def percentage(numerator: int, denominator: int) -> str:
    if denominator == 0:
        return "n/a (no eligible mutants)"
    return f"{numerator / denominator * 100:.2f}%"


def markdown_cell(value: Any) -> str:
    return str(value).replace("|", r"\|").replace("\r", " ").replace("\n", " ")


def build_summary(
    report: dict[str, Any],
    project: str,
    mode: str,
    since_ref: str,
    outcome: str,
) -> str:
    files = report.get("files") or {}
    test_files = report.get("testFiles") or {}

    status_counts: Counter[str] = Counter()
    operator_counts: Counter[str] = Counter()
    ignored_reasons: Counter[str] = Counter()
    error_reasons: Counter[str] = Counter()
    file_rows: list[tuple[int, str, Counter[str]]] = []
    project_root = Path(report.get("projectRoot", "")).resolve()
    covered_test_ids: set[str] = set()
    killing_test_ids: set[str] = set()
    tests_by_id: dict[str, str] = {}
    tests_by_file: dict[str, list[str]] = {}

    for test_file, test_file_data in test_files.items():
        try:
            display_test_file = Path(test_file).resolve().relative_to(
                project_root.parent.parent
            ).as_posix()
        except ValueError:
            display_test_file = Path(test_file).name

        tests_by_file[display_test_file] = []
        for test in test_file_data.get("tests", []):
            test_id = test.get("id")
            if test_id:
                tests_by_id[test_id] = test.get("name", test_id)
                tests_by_file[display_test_file].append(test_id)

    for source_file, file_data in files.items():
        counts: Counter[str] = Counter()
        for mutant in file_data.get("mutants", []):
            status = mutant.get("status", "Unknown")
            status_counts[status] += 1
            counts[status] += 1
            operator_counts[mutant.get("mutatorName", "Unknown")] += 1
            reason = mutant.get("statusReason") or "No reason provided"
            if status == "Ignored":
                ignored_reasons[reason] += 1
            elif status in ("CompileError", "RuntimeError"):
                error_reasons[reason] += 1
            covered_test_ids.update(mutant.get("coveredBy") or [])
            killing_test_ids.update(mutant.get("killedBy") or [])

        try:
            display_path = Path(source_file).resolve().relative_to(project_root).as_posix()
        except ValueError:
            display_path = source_file
        file_rows.append((counts["Survived"] + counts["NoCoverage"], display_path, counts))

    all_test_ids = set(tests_by_id)
    covered_test_ids &= all_test_ids
    killing_test_ids &= all_test_ids

    killing_tests = len(killing_test_ids)
    covering_only_tests = len(covered_test_ids - killing_test_ids)
    not_covering_tests = len(all_test_ids - covered_test_ids)
    total_tests = len(all_test_ids)
    test_file_rows = []
    for test_file, test_ids in sorted(tests_by_file.items()):
        file_killing = sum(test_id in killing_test_ids for test_id in test_ids)
        file_covering = sum(
            test_id in covered_test_ids and test_id not in killing_test_ids
            for test_id in test_ids
        )
        file_not_covering = len(test_ids) - file_killing - file_covering
        test_file_rows.append(
            (test_file, len(test_ids), file_killing, file_covering, file_not_covering)
        )

    killed = status_counts["Killed"]
    survived = status_counts["Survived"]
    timed_out = status_counts["Timeout"]
    no_coverage = status_counts["NoCoverage"]
    compile_errors = status_counts["CompileError"]
    runtime_errors = status_counts["RuntimeError"]
    ignored = status_counts["Ignored"]
    pending = status_counts["Pending"]

    detected = killed + timed_out
    undetected = survived + no_coverage
    valid = detected + undetected
    invalid = compile_errors + runtime_errors
    total_mutants = sum(status_counts.values())
    covered = detected + survived

    score = percentage(detected, valid)
    covered_score = percentage(detected, covered)

    since_filtered = sum(
        count for reason, count in ignored_reasons.items()
        if "since filter" in reason.casefold()
    )
    if valid == 0 and ignored > 0 and mode == "incremental" and since_filtered == ignored:
        run_note = (
            "No eligible mutants were executed: all ignored mutants were excluded by "
            "--since. This is not a 0% mutation score."
        )
    elif valid == 0 and ignored > 0:
        run_note = (
            "No eligible mutants were executed. See the ignored-mutant reasons below; "
            "this is not a 0% mutation score."
        )
    elif total_mutants == 0:
        run_note = "The report contains no mutants. Check the mutation filters and target project."
    else:
        run_note = "Mutation statistics are based on the statuses recorded in the Stryker JSON report."

    lines = [
        f"## Stryker.NET — {markdown_cell(project)}",
        "",
        f"- **Run mode:** {markdown_cell(mode)}",
        f"- **Base reference:** {markdown_cell(since_ref or 'full run')}",
        f"- **Stryker step:** {markdown_cell(outcome)}",
        f"- **Source files in report:** {len(files)}",
        f"- **Test source files:** {len(test_files)}",
        "",
        "### Test inventory and mutation impact",
        "",
        "| Metric | Count | Description |",
        "|---|---:|---|",
        f"| Discovered test cases | {total_tests} | Unique test IDs reported by the MTP runner |",
        f"| Tests that killed at least one mutant | {killing_tests} | At least one mutation caused this test to fail |",
        f"| Tests that covered mutants only | {covering_only_tests} | Covered mutants, but killed none |",
        f"| Tests that cover no mutants | {not_covering_tests} | No mutant was recorded as covered by this test |",
        f"| Tests with mutation coverage | {killing_tests + covering_only_tests} | Killing + covering-only tests |",
        "",
    ]

    if test_file_rows:
        lines.extend(
            [
                "### Test cases by source file",
                "",
                "| Test source file | Test cases | Killing | Covering only | No mutant coverage |",
                "|---|---:|---:|---:|---:|",
            ]
        )
        for test_file, count, killing, covering, not_covering in test_file_rows:
            lines.append(
                f"| {markdown_cell(test_file)} | {count} | {killing} | {covering} | {not_covering} |"
            )
        lines.append("")

    lines.extend(
        [
            "### Mutant outcomes",
            "",
            "| Status | Count | Meaning |",
            "|---|---:|---|",
            f"| Killed | {killed} | Test suite detected the mutation |",
            f"| Survived | {survived} | Mutation was covered, but all relevant tests passed |",
            f"| Timeout | {timed_out} | Test execution timed out; counted as detected by Stryker |",
            f"| No coverage | {no_coverage} | No test covered the mutated code |",
            f"| Compile error | {compile_errors} | Mutant could not be compiled; excluded from score |",
            f"| Runtime error | {runtime_errors} | Mutant run failed for a runtime reason; excluded from score |",
            f"| Ignored | {ignored} | Mutant was filtered/ignored and not evaluated |",
            f"| Pending | {pending} | Mutant was generated but not evaluated yet |",
            f"| **Total** | **{total_mutants}** | All statuses in this report |",
            "",
            "### Mutation scores",
            "",
            "| Metric | Result | Formula |",
            "|---|---:|---|",
            f"| Mutation score | **{score}** | (Killed + Timeout) / (Killed + Survived + Timeout + NoCoverage) |",
            f"| Covered-code mutation score | **{covered_score}** | (Killed + Timeout) / (Killed + Survived) |",
            f"| Detected mutants | {detected} | Killed + Timeout |",
            f"| Undetected mutants | {undetected} | Survived + NoCoverage |",
            f"| Valid mutants | {valid} | Detected + Undetected |",
            f"| Invalid mutants | {invalid} | Compile errors + Runtime errors |",
            "",
            f"> {run_note}",
            "",
        ]
    )

    if file_rows:
        rows = sorted(file_rows, key=lambda item: (-item[0], item[1]))
        rows_with_gaps = [row for row in rows if row[0] > 0][:15]
        if rows_with_gaps:
            lines.extend(
                [
                    "### Files with undetected mutations",
                    "",
                    "| Source file | Survived | No coverage | Killed | Timeout | Undetected total |",
                    "|---|---:|---:|---:|---:|---:|",
                ]
            )
            for undetected_count, source_file, counts in rows_with_gaps:
                lines.append(
                    "| "
                    + " | ".join(
                        [
                            markdown_cell(source_file),
                            str(counts["Survived"]),
                            str(counts["NoCoverage"]),
                            str(counts["Killed"]),
                            str(counts["Timeout"]),
                            str(undetected_count),
                        ]
                    )
                    + " |"
                )
            lines.append("")

    if ignored_reasons:
        lines.extend(
            [
                "### Ignored mutant reasons",
                "",
                "| Reason | Mutants |",
                "|---|---:|",
            ]
        )
        for reason, count in ignored_reasons.most_common(15):
            lines.append(f"| {markdown_cell(reason)} | {count} |")
        lines.append("")

    if error_reasons:
        lines.extend(
            [
                "### Mutant execution errors",
                "",
                "| Reason | Mutants |",
                "|---|---:|",
            ]
        )
        for reason, count in error_reasons.most_common(15):
            lines.append(f"| {markdown_cell(reason)} | {count} |")
        lines.append("")

    if operator_counts:
        lines.extend(
            [
                "### Mutation operators",
                "",
                "| Operator | Mutants |",
                "|---|---:|",
            ]
        )
        for operator, count in operator_counts.most_common(15):
            lines.append(f"| {markdown_cell(operator)} | {count} |")
        lines.append("")

    tests_without_mutation_coverage = sorted(
        tests_by_id[test_id]
        for test_id in all_test_ids - covered_test_ids
    )
    if tests_without_mutation_coverage:
        lines.extend(
            [
                "### Tests that did not cover any mutant",
                "",
            ]
        )
        lines.extend(f"- {markdown_cell(name)}" for name in tests_without_mutation_coverage[:20])
        if len(tests_without_mutation_coverage) > 20:
            lines.append(f"- … and {len(tests_without_mutation_coverage) - 20} more")
        lines.append("")

    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", required=True, type=Path)
    parser.add_argument("--project", required=True)
    parser.add_argument("--mode", choices=("full", "incremental"), required=True)
    parser.add_argument("--since-ref", default="")
    parser.add_argument("--stryker-outcome", default="unknown")
    parser.add_argument("--summary-file", type=Path)
    args = parser.parse_args()

    if not args.report.is_file():
        message = (
            f"Stryker JSON report not found: {args.report}. "
            f"Stryker step outcome: {args.stryker_outcome}."
        )
        print(f"::warning::{message}")
        if args.stryker_outcome == "success":
            return 1
        if args.summary_file:
            args.summary_file.parent.mkdir(parents=True, exist_ok=True)
            with args.summary_file.open("a", encoding="utf-8") as summary:
                summary.write(f"## Stryker.NET — {markdown_cell(args.project)}\n\n")
                summary.write(f"> {markdown_cell(message)}\n\n")
        return 0

    try:
        report = json.loads(args.report.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        print(f"::error::Unable to read Stryker report: {exc}")
        return 1

    if not isinstance(report, dict) or "files" not in report:
        print("::error::Stryker JSON report has an unexpected structure (missing 'files').")
        return 1

    summary = build_summary(
        report,
        args.project,
        args.mode,
        args.since_ref,
        args.stryker_outcome,
    )

    print(summary)
    summary_path = args.summary_file
    if summary_path is None and os.environ.get("GITHUB_STEP_SUMMARY"):
        summary_path = Path(os.environ["GITHUB_STEP_SUMMARY"])

    if summary_path:
        summary_path.parent.mkdir(parents=True, exist_ok=True)
        with summary_path.open("a", encoding="utf-8") as output:
            output.write(summary)
            output.write("\n")

    return 0


if __name__ == "__main__":
    sys.exit(main())
