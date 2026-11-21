<#
.SYNOPSIS
    Перегенерировать контракт оверлейного хаба: JSON-манифест и TypeScript-карту событий.

.DESCRIPTION
    Источник истины — рефлексия по TelegramusEvent и сигнатурам IAlertsHub.
    C#-тест с MARS_REGEN_OVERLAY=1 переписывает манифест, node-скрипт — overlayEvents.ts.
    Обычные тесты только сверяют файлы и падают при расхождении.
#>
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$env:MARS_REGEN_OVERLAY = "1"
try {
    dotnet test tests/MARS.Alerts.Tests/MARS.Alerts.Tests.csproj -- --filter-class '*OverlayContractGeneratorTests*'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Remove-Item Env:MARS_REGEN_OVERLAY -ErrorAction SilentlyContinue
}

Push-Location "src/MARS.ClientUi"
try {
    node ./scripts/generate-overlay-events.mjs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
