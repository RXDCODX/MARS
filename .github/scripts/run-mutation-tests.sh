#!/usr/bin/env bash
set -euo pipefail

# Keep the workflow entry point stable; Python runs the per-target process pool,
# streams prefixed logs and writes one consolidated status.tsv for ci-summary.py.
exec python3 "$GITHUB_WORKSPACE/.github/scripts/run-mutation-tests.py"
