#!/usr/bin/env python3
"""Aggregate normal TRX test results and Stryker JSON reports for CI/PR."""

from __future__ import annotations

import argparse
import json
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path
import sys
from typing import Any


def md(value: Any) -> str:
    return str(value).replace("|", r"\|").replace("\r", " ").replace("\n", " ")


def code(value: str) -> str:
    return chr(96) + md(value) + chr(96)


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def int_attr(element: ET.Element | None, name: str) -> int:
    if element is None:
        return 0
    try:
        return int(element.attrib.get(name, "0"))
    except ValueError:
        return 0


def parse_trx(path: Path) -> dict[str, Any]:
    root = ET.parse(path).getroot()
    counters = next((node for node in root.iter() if local_name(node.tag) == "Counters"), None)
    results = [node for node in root.iter() if local_name(node.tag) == "UnitTestResult"]

    if counters is not None:
        total = int_attr(counters, "total")
        executed = int_attr(counters, "executed")
        passed = int_attr(counters, "passed")
        failed = int_attr(counters, "failed")
        errors = int_attr(counters, "error")
        timeouts = int_attr(counters, "timeout")
        aborted = int_attr(counters, "aborted")
        skipped = int_attr(counters, "notExecuted") + int_attr(counters, "inconclusive")
    else:
        outcomes = Counter(node.attrib.get("outcome", "Unknown") for node in results)
        total = len(results)
        passed = outcomes["Passed"]
        failed = outcomes["Failed"]
        errors = outcomes["Error"]
        timeouts = outcomes["Timeout"]
        aborted = outcomes["Aborted"]
        skipped = outcomes["NotExecuted"] + outcomes["Inconclusive"]
        executed = total - skipped

    failures: list[tuple[str, str, str]] = []
    for result in results:
        outcome = result.attrib.get("outcome", "")
        if outcome not in {"Failed", "Error", "Timeout", "Aborted"}:
            continue
        test_name = result.attrib.get("testName", "(unnamed test)")
        message = ""
        stack = ""
        for node in result.iter():
            name = local_name(node.tag)
            if name == "Message" and not message:
                message = (node.text or "").strip()
            elif name == "StackTrace" and not stack:
                stack = (node.text or "").strip()
        failures.append((test_name, outcome, message or stack))

    return {
        "total": total, "executed": executed, "passed": passed,
        "failed": failed, "errors": errors, "timeouts": timeouts,
        "aborted": aborted, "skipped": skipped, "failures": failures,
    }


def load_test_projects(root: Path) -> tuple[list[dict[str, Any]], list[tuple[str, str, str]]]:
    projects: dict[str, dict[str, Any]] = {}
    failures: list[tuple[str, str, str]] = []
    if not root.exists():
        return [], failures

    for artifact_dir in sorted(path for path in root.iterdir() if path.is_dir()):
        status_files = list(artifact_dir.rglob("project-status.json"))
        project = artifact_dir.name.removeprefix("coverage-")
        exit_code: int | None = None
        if status_files:
            try:
                status = json.loads(status_files[0].read_text(encoding="utf-8"))
                project = status.get("project", project)
                exit_code = int(status.get("exit_code", 1))
            except (OSError, ValueError, json.JSONDecodeError):
                exit_code = 1

        trx_files = sorted(artifact_dir.rglob("*.trx"))
        if not status_files and not trx_files:
            # The coverage-html artifact also matches coverage-*; it is not a test project.
            continue
        totals = Counter()
        project_failures: list[tuple[str, str, str]] = []
        for trx in trx_files:
            try:
                parsed = parse_trx(trx)
            except (ET.ParseError, OSError) as exc:
                project_failures.append((trx.name, "TRX parse error", str(exc)))
                continue
            for name in (
                "total", "executed", "passed", "failed", "errors",
                "timeouts", "aborted", "skipped",
            ):
                totals[name] += parsed[name]
            project_failures.extend(parsed["failures"])

        if exit_code is None:
            result = "unknown" if trx_files else "no result"
        else:
            result = "passed" if exit_code == 0 else "failed"

        projects[project] = {
            "project": project, "result": result, "exit_code": exit_code,
            "trx_files": len(trx_files), "counts": dict(totals),
            "failures": project_failures,
        }
        for failure in project_failures:
            failures.append((project, failure[0], f"{failure[1]}: {failure[2]}"))

    expected_projects = [
        "MARS.Admin.Tests", "MARS.Alerts.Tests", "MARS.AudioController.Tests",
        "MARS.CinemaQueue.Tests", "MARS.Commands.Tests", "MARS.Discord.Tests",
        "MARS.Gateway.Tests", "MARS.MediaStorage.Tests", "MARS.OBS.Tests",
        "MARS.Scoreboard.Tests", "MARS.Shared.Tests", "MARS.Shikimori.Tests",
        "MARS.SoundRequest.Tests", "MARS.Telegram.Tests", "MARS.TTS.Tests",
        "MARS.TwitchCore.Tests", "MARS.Videos365.Tests", "MARS.WaifuGacha.Tests",
    ]
    for project in expected_projects:
        projects.setdefault(project, {
            "project": project, "result": "no artifact", "exit_code": None,
            "trx_files": 0,
            "counts": {
                "total": 0, "executed": 0, "passed": 0, "failed": 0,
                "errors": 0, "timeouts": 0, "aborted": 0, "skipped": 0,
            },
            "failures": [],
        })
    return sorted(projects.values(), key=lambda item: item["project"]), failures


def read_mutation_status(path: Path) -> dict[str, dict[str, str]]:
    statuses: dict[str, dict[str, str]] = {}
    if not path.is_file():
        return statuses
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line or line.startswith("target\t"):
            continue
        parts = line.split("\t")
        if len(parts) >= 4:
            statuses[parts[0]] = {
                "test_project": parts[1], "result": parts[2], "exit_code": parts[3],
            }
    return statuses


def parse_mutation_report(path: Path, status: dict[str, str]) -> dict[str, Any]:
    report = json.loads(path.read_text(encoding="utf-8"))
    files = report.get("files") or {}
    test_files = report.get("testFiles") or {}
    counts: Counter[str] = Counter()
    covered_ids: set[str] = set()
    killed_ids: set[str] = set()
    test_names: dict[str, str] = {}

    for test_file_data in test_files.values():
        for test in test_file_data.get("tests", []):
            test_id = test.get("id")
            if test_id:
                test_names[test_id] = test.get("name", test_id)

    for file_data in files.values():
        for mutant in file_data.get("mutants", []):
            counts[mutant.get("status", "Unknown")] += 1
            covered_ids.update(mutant.get("coveredBy") or [])
            killed_ids.update(mutant.get("killedBy") or [])

    all_test_ids = set(test_names)
    covered_ids &= all_test_ids
    killed_ids &= all_test_ids
    killed = counts["Killed"]
    survived = counts["Survived"]
    timeout = counts["Timeout"]
    no_coverage = counts["NoCoverage"]
    valid = killed + survived + timeout + no_coverage
    score = f"{(killed + timeout) / valid * 100:.2f}%" if valid else "n/a"

    return {
        "target": path.parents[1].name,
        "test_project": status.get("test_project", "unknown"),
        "result": status.get("result", "report found"),
        "test_cases": len(all_test_ids),
        "test_files": len(test_files),
        "killing_tests": len(killed_ids),
        "covering_only_tests": len(covered_ids - killed_ids),
        "uncovered_tests": len(all_test_ids - covered_ids),
        "mutants": counts,
        "valid_mutants": valid,
        "mutation_score": score,
        "files": len(files),
    }


def load_mutations(root: Path, status_path: Path) -> list[dict[str, Any]]:
    statuses = read_mutation_status(status_path)
    reports = sorted(root.rglob("mutation-report.json")) if root.exists() else []
    by_target: dict[str, dict[str, Any]] = {}
    for report_path in reports:
        try:
            target = report_path.parents[1].name
            by_target[target] = parse_mutation_report(report_path, statuses.get(target, {}))
        except (OSError, json.JSONDecodeError, IndexError, TypeError) as exc:
            target = report_path.parents[1].name if len(report_path.parents) > 1 else report_path.name
            by_target[target] = {
                "target": target, "test_project": statuses.get(target, {}).get("test_project", "unknown"),
                "result": f"report error: {exc}", "test_cases": 0, "test_files": 0,
                "killing_tests": 0, "covering_only_tests": 0, "uncovered_tests": 0,
                "mutants": Counter(), "valid_mutants": 0, "mutation_score": "n/a", "files": 0,
            }

    for target, status in statuses.items():
        if target not in by_target:
            by_target[target] = {
                "target": target, "test_project": status.get("test_project", "unknown"),
                "result": status.get("result", "unknown"), "test_cases": 0, "test_files": 0,
                "killing_tests": 0, "covering_only_tests": 0, "uncovered_tests": 0,
                "mutants": Counter(), "valid_mutants": 0, "mutation_score": "n/a", "files": 0,
            }
    return sorted(by_target.values(), key=lambda item: item["target"])


def render(args: argparse.Namespace, tests: list[dict[str, Any]],
           test_failures: list[tuple[str, str, str]],
           mutations: list[dict[str, Any]]) -> str:
    jobs = {
        "Build": args.build_result,
        "Backend test matrix": args.tests_result,
        "Mutation testing": args.mutation_result,
        "Frontend tests": args.frontend_result,
        "E2E": args.e2e_result,
        "Coverage gate": args.coverage_result,
    }
    hard_failures = [name for name, result in jobs.items() if result == "failure"]
    all_success = all(result == "success" for result in jobs.values())
    overall = "FAILED" if hard_failures else ("PASSED" if all_success else "PARTIAL / SKIPPED")

    lines = [
        "# MARS CI summary", "",
        f"**Overall: {overall}**", "",
        f"- Commit: {code(args.commit)}",
        f"- Workflow run: [#{md(args.run_id)}]({md(args.run_url)})",
        f"- Event: {md(args.event)}", "",
        "## Pipeline jobs", "", "| Job | Result |", "|---|---|",
    ]
    for name, result in jobs.items():
        lines.append(f"| {name} | **{md(result)}** |")

    lines.extend(["", "## Regular test results", ""])
    if tests:
        total = Counter()
        for item in tests:
            total.update(item["counts"])
        lines.extend([
            f"**Projects with TRX artifacts:** {len(tests)}", "",
            f"**Test cases:** {total['total']} total, {total['executed']} executed, "
            f"{total['passed']} passed, {total['failed']} failed, "
            f"{total['errors']} errors, {total['timeouts']} timeouts, "
            f"{total['skipped']} skipped.", "",
            "| Test project | Job result | TRX files | Total | Passed | Failed | Errors | Timeouts | Skipped |",
            "|---|---|---:|---:|---:|---:|---:|---:|---:|",
        ])
        for item in tests:
            c = item["counts"]
            lines.append(
                f"| {code(item['project'])} | {md(item['result'])} | {item['trx_files']} | "
                f"{c['total']} | {c['passed']} | {c['failed']} | {c['errors']} | "
                f"{c['timeouts']} | {c['skipped']} |"
            )
    else:
        lines.append("No TRX artifacts were found. Check test-job setup and artifact upload.")

    if test_failures:
        lines.extend(["", "### Failed / errored test cases", ""])
        for project, name, reason in test_failures[:50]:
            lines.append(f"- {code(project)} — {md(name)}: {md(reason[:500])}")
        if len(test_failures) > 50:
            lines.append(f"- … and {len(test_failures) - 50} more.")

    lines.extend(["", "## Mutation testing results", ""])
    if args.mutation_result == "skipped":
        lines.append(
            "Mutation testing was **skipped** because the build or regular backend test matrix "
            "did not succeed. This is intentional: Stryker is not run against a failing baseline."
        )
    elif mutations:
        total_mutants: Counter[str] = Counter()
        for item in mutations:
            total_mutants.update(item["mutants"])
        killed, survived = total_mutants["Killed"], total_mutants["Survived"]
        timeout, no_coverage = total_mutants["Timeout"], total_mutants["NoCoverage"]
        valid = killed + survived + timeout + no_coverage
        overall_score = f"{(killed + timeout) / valid * 100:.2f}%" if valid else "n/a"
        test_case_runs = sum(item["test_cases"] for item in mutations)
        lines.extend([
            f"**Targets:** {len(mutations)}",
            f"**Mutation score (aggregate):** {overall_score}",
            f"**Mutants:** {sum(total_mutants.values())} total; {killed} killed, "
            f"{survived} survived, {timeout} timed out, {no_coverage} without coverage, "
            f"{total_mutants['CompileError']} compile errors, "
            f"{total_mutants['RuntimeError']} runtime errors, "
            f"{total_mutants['Ignored']} ignored, {total_mutants['Pending']} pending.",
            f"**Discovered test-case instances across target runs:** {test_case_runs}. "
            "MARS.Shared.Tests is reused for 11 infrastructure targets, so this is not a "
            "count of unique test cases.", "",
            "| Target | Test project | Stryker | Test cases | Killing tests | Covering only | No mutation coverage | Killed | Survived | No coverage | Ignored | Mutation score |",
            "|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|",
        ])
        for item in mutations:
            m = item["mutants"]
            lines.append(
                f"| {code(item['target'])} | {code(item['test_project'])} | {md(item['result'])} | "
                f"{item['test_cases']} | {item['killing_tests']} | {item['covering_only_tests']} | "
                f"{item['uncovered_tests']} | {m['Killed']} | {m['Survived']} | "
                f"{m['NoCoverage']} | {m['Ignored']} | **{item['mutation_score']}** |"
            )
    else:
        lines.append(
            "No Stryker JSON reports were found. The mutation job may have been skipped or "
            "failed before it could produce an artifact."
        )

    lines.extend([
        "", "### Score interpretation", "",
        "- " + code("n/a") + " means that the run had no valid mutants to score (for example, all generated "
        "mutants were excluded by the incremental " + code("--since") + " filter). It is not 0%.",
        "- Aggregate mutation score is calculated from summed mutant statuses, not an average "
        "of per-project percentages.",
        "- The current configured break threshold is 0%, so mutation score does not fail CI yet.",
        "",
    ])
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--test-artifacts", type=Path, required=True)
    parser.add_argument("--mutation-results", type=Path, required=True)
    parser.add_argument("--mutation-status", type=Path, required=True)
    parser.add_argument("--output-file", type=Path, required=True)
    parser.add_argument("--step-summary", type=Path, required=True)
    parser.add_argument("--build-result", default="unknown")
    parser.add_argument("--tests-result", default="unknown")
    parser.add_argument("--mutation-result", default="unknown")
    parser.add_argument("--frontend-result", default="unknown")
    parser.add_argument("--e2e-result", default="unknown")
    parser.add_argument("--coverage-result", default="unknown")
    parser.add_argument("--commit", default="unknown")
    parser.add_argument("--run-id", default="unknown")
    parser.add_argument("--run-url", default="")
    parser.add_argument("--event", default="unknown")
    args = parser.parse_args()

    tests, failures = load_test_projects(args.test_artifacts)
    mutations = load_mutations(args.mutation_results, args.mutation_status)
    summary = render(args, tests, failures, mutations)

    args.output_file.parent.mkdir(parents=True, exist_ok=True)
    args.output_file.write_text(summary + "\n", encoding="utf-8")
    args.step_summary.parent.mkdir(parents=True, exist_ok=True)
    with args.step_summary.open("a", encoding="utf-8") as output:
        output.write(summary + "\n")
    print(summary)
    return 0


if __name__ == "__main__":
    sys.exit(main())
