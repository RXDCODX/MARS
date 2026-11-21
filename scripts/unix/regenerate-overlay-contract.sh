#!/usr/bin/env bash
# Перегенерировать контракт оверлейного хаба: JSON-манифест и TypeScript-карту событий.
# Источник истины — рефлексия по TelegramusEvent и сигнатурам IAlertsHub.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

MARS_REGEN_OVERLAY=1 dotnet test tests/MARS.Alerts.Tests/MARS.Alerts.Tests.csproj -- --filter-class '*OverlayContractGeneratorTests*'

cd "$ROOT/src/MARS.ClientUi"
node ./scripts/generate-overlay-events.mjs
