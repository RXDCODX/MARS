#!/usr/bin/env python3
"""Run the repository's Stryker targets with bounded parallelism."""

from __future__ import annotations

import asyncio
import os
import sys
from dataclasses import dataclass
from pathlib import Path


TARGETS: tuple[tuple[str, str], ...] = (
    ("MARS.Admin.Tests", "MARS.Admin"),
    ("MARS.Alerts.Tests", "MARS.Alerts"),
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


async def run_target(
    workspace: Path,
    output_root: Path,
    config_file: Path,
    since: str,
    test_project: str,
    target: str,
    semaphore: asyncio.Semaphore,
    project_locks: dict[str, asyncio.Lock],
    status_lock: asyncio.Lock,
    status_file: Path,
) -> Result:
    # Several MARS.Platform.* targets share MARS.Shared.Tests. Serialize those
    # invocations to avoid concurrent MSBuild writes into the same test-project
    # obj/bin directories. The lock is acquired before the global semaphore so
    # waiting shared targets do not occupy worker slots.
    async with project_locks[test_project]:
        async with semaphore:
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

            print(
                f"[{target}] starting (tests: {test_project})",
                flush=True,
            )
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


async def main() -> int:
    workspace = Path(os.environ.get("GITHUB_WORKSPACE") or Path.cwd()).resolve()
    output_root = Path(
        os.environ.get("STRYKER_OUTPUT")
        or os.environ.get("RUNNER_TEMP", "/tmp")
    ).resolve() / (
        "" if os.environ.get("STRYKER_OUTPUT") else "stryker"
    )
    output_root.mkdir(parents=True, exist_ok=True)
    config_file = workspace / ".config" / "stryker-config.json"
    status_file = output_root / "status.tsv"
    status_file.write_text(
        "target\ttest_project\tresult\texit_code\n",
        encoding="utf-8",
    )

    max_parallel = read_positive_int(
        "STRYKER_MAX_PARALLEL",
        min(4, os.cpu_count() or 1),
    )
    since = os.environ.get("STRYKER_SINCE", "").strip()

    print(
        f"Running {len(TARGETS)} Stryker targets with up to "
        f"{max_parallel} concurrent processes; each Stryker uses one worker.",
        flush=True,
    )

    semaphore = asyncio.Semaphore(max_parallel)
    project_locks = {
        project: asyncio.Lock()
        for project in {test_project for test_project, _ in TARGETS}
    }
    status_lock = asyncio.Lock()

    results = await asyncio.gather(
        *(
            run_target(
                workspace,
                output_root,
                config_file,
                since,
                test_project,
                target,
                semaphore,
                project_locks,
                status_lock,
                status_file,
            )
            for test_project, target in TARGETS
        )
    )

    succeeded = sum(result.exit_code == 0 for result in results)
    failed = len(results) - succeeded
    print(
        f"Stryker targets finished: {succeeded} succeeded, {failed} failed.",
        flush=True,
    )
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
