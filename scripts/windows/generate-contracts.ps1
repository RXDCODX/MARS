<#
.SYNOPSIS
    Генерирует офлайн-снимки контрактов MARS.Alerts (OpenAPI REST + SignalR hubs)
    для фронтенда MARS.ClientUi.

.DESCRIPTION
    Запускает MARS.Alerts.Api.Specs: собирает приложение Alerts в памяти, берёт
    документы ISwaggerProvider ("v1" и "hubs") и пишет их в каталог api
    фронтенда (по умолчанию src/MARS.ClientUi/api). Сервисы поднимать не надо —
    генерация офлайн. Результат коммитится и сверяется гейтом check-contracts.

.PARAMETER Output
    Каталог для снимков. По умолчанию src/MARS.ClientUi/api.

.EXAMPLE
    .\scripts\windows\generate-contracts.ps1
    Пересобрать снимки контрактов Alerts.

.EXAMPLE
    .\scripts\windows\generate-contracts.ps1 -Output C:\tmp\contracts
    Сгенерировать в произвольный каталог.
#>
[CmdletBinding()]
param(
    [string]$Output
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $script:MarsRoot "src\MARS.ClientUi\api"
}

$specsProject = Join-Path $script:MarsRoot "src\MARS.Alerts\MARS.Alerts.Api.Specs\MARS.Alerts.Api.Specs.csproj"

Write-Step "Генерация контрактов MARS.Alerts"

$arguments = @(
    "run",
    "--project", $specsProject,
    "--configuration", "Release",
    "--",
    "--output", $Output
)

$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"

& dotnet @arguments

$dotnetExitCode = $LASTEXITCODE

$ErrorActionPreference = $previousErrorActionPreference

if ($dotnetExitCode -ne 0) {
    throw "dotnet run завершился с кодом $dotnetExitCode"
}

Write-Success "Снимки контрактов обновлены в $Output"