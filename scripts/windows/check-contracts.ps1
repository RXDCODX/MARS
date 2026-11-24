<#
.SYNOPSIS
    Проверяет актуальность снимков контрактов MARS.Alerts в MARS.ClientUi.

.DESCRIPTION
    Регенерирует снимки во временный каталог и сравнивает с коммиченными
    файлами src/MARS.ClientUi/api/swagger_alerts_v1.json и
    swagger_alerts_hubs.json. При расхождении печатает список файлов и
    подсказку запустить generate-contracts. Работает офлайн.

.EXAMPLE
    .\scripts\windows\check-contracts.ps1
    Проверить актуальность контрактов Alerts.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$tempDir = Join-Path $env:TEMP ("mars-contracts-check-" + [Guid]::NewGuid().ToString("N"))
$committedDir = Join-Path $script:MarsRoot "src\MARS.ClientUi\api"

$files = @("swagger_alerts_v1.json", "swagger_alerts_hubs.json")

try {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    $generateScript = Join-Path $PSScriptRoot "generate-contracts.ps1"
    & $generateScript -Output $tempDir

    if ($LASTEXITCODE -ne 0) {
        throw "Генерация контрактов завершилась с кодом $LASTEXITCODE"
    }

    $failed = $false

    foreach ($file in $files) {
        $committed = Join-Path $committedDir $file
        $generated = Join-Path $tempDir $file

        if (-not (Test-Path -LiteralPath $committed)) {
            Write-Note "Коммиченный снимок отсутствует: $committed"
            $failed = $true
            continue
        }

        if (-not (Test-Path -LiteralPath $generated)) {
            Write-Note "Сгенерированный файл отсутствует: $generated"
            $failed = $true
            continue
        }

        $committedHash = (Get-FileHash -LiteralPath $committed -Algorithm SHA256).Hash
        $generatedHash = (Get-FileHash -LiteralPath $generated -Algorithm SHA256).Hash

        if ($committedHash -ne $generatedHash) {
            Write-Note "Расхождение контракта: $file"
            $failed = $true
        }
    }

    if ($failed) {
        Write-Failure "Контракты Alerts устарели. Запусти .\scripts\windows\generate-contracts.ps1 и закоммить результат."
        exit 1
    }

    Write-Success "Контракты Alerts актуальны"
}
finally {
    if (Test-Path -LiteralPath $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}