#!/usr/bin/env python3
"""Run all Stryker targets in a bounded pool of isolated Git worktrees."""

from __future__ import annotations

import asyncio
import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


TARGETS: tuple[tuple[str, str], ...] = (
    ("MARS.Admin.Tests", "MARS.Admin"),
    ("MARS.Alerts.Tests", "MARS.Alerts"),
    ("MARS.Alerts.Tests", "MARS.Alerts.Twitch.Rewards.Application"),
    ("MARS.Alerts.Tests", "MARS.Alerts.Twitch.Alerts.Application"),
    ("MARS.Alerts.Tests", "MARS.Alerts.Overlay.Application"),
    ("MARS.AudioController.Tests", "MARS.AudioController"),
    ("MARS.CinemaQueue.Tests", "MARS.CinemaQueue"),
    ("MARS.Commands.Tests", "MARS.Commands"),
    ("MARS.Discord.Tests", "MARS.Discord"),
    ("MARS.Gateway.Tests", "MARS.Gateway"),
    ("MARS.MediaStorage.Tests", "MARS.MediaStorage"),
    ("MARS.OBS.Tests", "MARS.OBS"),
    ("MARS.Scoreboard.Tests", "MARS.Scoreboard"),
    ("MARS.Shikimori.Tests", "MARS.Shikimori"),
    ("MARS.SoundRequest.Tests", "MARS.SoundRequest"),
    ("MARS.Telegram.Tests", "MARS.Telegram"),
    ("MARS.TTS.Tests", "MARS.TTS"),
    ("MARS.TwitchCore.Tests", "MARS.TwitchCore"),
    ("MARS.Videos365.Tests", "MARS.Videos365"),
    ("MARS.WaifuGacha.Tests", "MARS.WaifuGacha"),
    ("MARS.Shared.Tests", "MARS.Shared.Kernel"),
    ("MARS.Shared.Tests", "MARS.Shared.Contracts"),
    ("MARS.Shared.Tests", "MARS.Platform.Clients.Http.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Development.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Grpc.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Hosting.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Messaging.RabbitMq.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Observability.Logging.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Observability.Telemetry.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Persistence.Postgres.Infrastructure"),
    ("MARS.Shared.Tests", "MARS.Platform.Web.AspNetCore.Infrastructure"),
)


@dataclass(frozen=True)
class Result:
    test_project: str
    target: str
    exit_code: int

    @property
    def status(self) -> str:
        return "success" if self.exit_code == 0 else "failure"


def read_positive_int(name: str, default: int) -> int:
    raw = os.environ.get(name, str(default))
    try:
        value = int(raw)
    except ValueError as exc:
        raise ValueError(f"{name} must be a positive integer, got {raw!r}") from exc
    if value < 1:
        raise ValueError(f"{name} must be a positive integer, got {value}")
    return value


def create_worktree(workspace: Path, path: Path, commit: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run(
        [
            "git", "-C", str(workspace), "worktree", "add",
            "--detach", "--quiet", str(path), commit,
        ],
        check=True,
        text=True,
        capture_output=True,
    )


def remove_worktree(workspace: Path, path: Path) -> None:
    completed = subprocess.run(
        [
            "git", "-C", str(workspace), "worktree", "remove",
            "--force", str(path),
        ],
        text=True,
        capture_output=True,
        check=False,
    )
    if completed.returncode != 0:
        print(
            f"::warning::Could not remove worktree {path}: "
            f"{completed.stderr.strip()}",
            flush=True,
        )


async def run_target(
    workspace: Path,
    output_root: Path,
    config_file: Path,
    since: str,
    test_project: str,
    target: str,
    status_lock: asyncio.Lock,
    status_file: Path,
) -> Result:
    project_output = output_root / target
    project_output.mkdir(parents=True, exist_ok=True)
    log_file = project_output / "stryker.log"

    command = [
        "dotnet",
        "stryker",
        "--config-file",
        str(config_file),
        "--project",
        f"{target}.csproj",
        "--configuration",
        "Release",
        "--break-on-initial-test-failure",
        "--concurrency",
        "1",
        "--output",
        str(project_output),
    ]
    if since:
        command.append(f"--since:{since}")

    print(f"[{target}] starting (tests: {test_project})", flush=True)
    exit_code = 127
    try:
        process = await asyncio.create_subprocess_exec(
            *command,
            cwd=workspace / "tests" / test_project,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.STDOUT,
        )
        assert process.stdout is not None
        with log_file.open("wb") as log:
            async for raw_line in process.stdout:
                log.write(raw_line)
                decoded = raw_line.decode("utf-8", errors="replace").rstrip("\r\n")
                print(f"[{target}] {decoded}", flush=True)
        exit_code = await process.wait()
    except OSError as exc:
        message = f"Could not start Stryker: {exc}\n"
        log_file.write_text(message, encoding="utf-8")
        print(f"[{target}] {message.rstrip()}", file=sys.stderr, flush=True)

    result = Result(test_project, target, exit_code)
    async with status_lock:
        with status_file.open("a", encoding="utf-8") as status:
            status.write(
                f"{result.target}\t{result.test_project}\t"
                f"{result.status}\t{result.exit_code}\n"
            )

    if exit_code == 0:
        print(f"[{target}] completed successfully", flush=True)
    else:
        print(
            f"::error::Stryker failed for {target} (exit code {exit_code})",
            flush=True,
        )
    return result


async def run_worker(
    worker_id: int,
    workspace: Path,
    queue: asyncio.Queue[tuple[str, str]],
    output_root: Path,
    config_file: Path,
    since: str,
    status_lock: asyncio.Lock,
    status_file: Path,
) -> list[Result]:
    results: list[Result] = []
    while True:
        try:
            test_project, target = queue.get_nowait()
        except asyncio.QueueEmpty:
            break
        try:
            print(f"[worker-{worker_id}] picked {target}", flush=True)
            result = await run_target(
                workspace,
                output_root,
                config_file,
                since,
                test_project,
                target,
                status_lock,
                status_file,
            )
            results.append(result)
        finally:
            queue.task_done()
    return results


async def main() -> int:
    workspace = Path(os.environ.get("GITHUB_WORKSPACE") or Path.cwd()).resolve()
    output_value = os.environ.get("STRYKER_OUTPUT")
    if output_value:
        output_root = Path(output_value).resolve()
    else:
        output_root = Path(os.environ.get("RUNNER_TEMP", "/tmp")).resolve() / "stryker"
    output_root.mkdir(parents=True, exist_ok=True)

    config_file = workspace / ".config" / "stryker-config.json"
    status_file = output_root / "status.tsv"
    status_file.write_text(
        "target\ttest_project\tresult\texit_code\n",
        encoding="utf-8",
    )

    max_parallel = min(
        read_positive_int(
            "STRYKER_MAX_PARALLEL",
            min(4, os.cpu_count() or 1),
        ),
        len(TARGETS),
    )
    since = os.environ.get("STRYKER_SINCE", "").strip()
    commit = os.environ.get("GITHUB_SHA", "").strip()
    if not commit:
        commit = subprocess.check_output(
            ["git", "-C", str(workspace), "rev-parse", "HEAD"],
            text=True,
        ).strip()

    temp_root = Path(os.environ.get("RUNNER_TEMP", "/tmp")).resolve()
    worktree_root = temp_root / "stryker-worktrees"
    worktrees: list[Path] = []

    print(
        f"Running {len(TARGETS)} Stryker targets with up to "
        f"{max_parallel} concurrent processes. Each process uses one Stryker "
        "worker and an isolated Git worktree.",
        flush=True,
    )

    try:
        # A worker owns its worktree for its whole lifetime. This isolates bin/obj
        # for targets that reuse MARS.Shared.Tests, while avoiding 28 full copies.
        for index in range(max_parallel):
            worktree = worktree_root / f"worker-{index + 1}"
            create_worktree(workspace, worktree, commit)
            worktrees.append(worktree)
            print(f"Created isolated worktree: {worktree}", flush=True)

        queue: asyncio.Queue[tuple[str, str]] = asyncio.Queue()
        for target in TARGETS:
            queue.put_nowait(target)

        status_lock = asyncio.Lock()
        worker_results = await asyncio.gather(
            *(
                run_worker(
                    index + 1,
                    worktree,
                    queue,
                    output_root,
                    config_file,
                    since,
                    status_lock,
                    status_file,
                )
                for index, worktree in enumerate(worktrees)
            )
        )
    finally:
        for worktree in reversed(worktrees):
            remove_worktree(workspace, worktree)

    results = [result for group in worker_results for result in group]
    succeeded = sum(result.exit_code == 0 for result in results)
    failed = len(TARGETS) - succeeded
    print(
        f"Stryker targets finished: {succeeded} succeeded, {failed} failed.",
        flush=True,
    )
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
