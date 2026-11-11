#!/usr/bin/env python3
"""Validate every JSON file tracked by Git using the strict JSON grammar."""

from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path
from typing import Any


def reject_nonstandard_constant(value: str) -> None:
    raise ValueError(f"Non-standard JSON constant: {value}")


def tracked_json_files() -> list[Path]:
    result = subprocess.run(
        ["git", "ls-files", "-z"],
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return [
        Path(os.fsdecode(path))
        for path in result.stdout.split(b"\0")
        if path and os.fsdecode(path).lower().endswith(".json")
    ]


def main() -> int:
    try:
        files = tracked_json_files()
    except subprocess.CalledProcessError as exc:
        detail = exc.stderr.decode("utf-8", errors="replace").strip()
        print(f"::error::Unable to enumerate tracked files: {detail}", file=sys.stderr)
        return 2

    if not files:
        print("::error::No tracked JSON files were found.", file=sys.stderr)
        return 1

    errors: list[str] = []
    for path in files:
        try:
            with path.open("r", encoding="utf-8-sig") as source:
                json.load(source, parse_constant=reject_nonstandard_constant)
        except (OSError, UnicodeError, json.JSONDecodeError, ValueError) as exc:
            if isinstance(exc, json.JSONDecodeError):
                location = f"line {exc.lineno}, column {exc.colno}: {exc.msg}"
            else:
                location = str(exc)
            errors.append(f"{path}: {location}")

    if errors:
        for error in errors:
            print(f"::error file={error.split(':', 1)[0]}::{error}")
        print(f"Invalid JSON: {len(errors)} of {len(files)} tracked files.")
        return 1

    print(f"Validated {len(files)} tracked JSON files successfully.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
