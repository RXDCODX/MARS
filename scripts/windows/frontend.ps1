<#
.SYNOPSIS
    Frontend Gateway: type checks, tests and build.

.DESCRIPTION
    UI MediaStorage реализован через ASP.NET Core Razor Pages и vanilla JS
    внутри MARS.MediaStorage.Host. Отдельного npm/Vite frontend нет; единственный
    отдельно собираемый JS-клиент — src/MARS.ClientUi.

    Для клиента шлюза `npm ci` нерабочий: он требует `package-lock.json`,
    которого в репозитории нет, и не выполняет postinstall-скрипты, которые
    разрешает `.yarnrc.yml`. Второй lock-файл рядом с первым — это два
    источника правды, и собирать надо тем же инструментом, которым собирается
    образ. Образ `client-ui` собирает ровно так же: `corepack enable && yarn
    install --immutable`.

    `SKIP_STORYBOOK_VITEST=1` обязателен: без него vitest поднимает сюжеты
    Storybook, а тот ставит Playwright и качает Chromium — минуты загрузки и
    лишний браузер без единой проверки.

    `yarn test` в манифесте — это `vitest` в watch-режиме: без `run` он не
    завершится и скрипт зависнет.

.PARAMETER Client
    gateway или all — клиент шлюза (Yarn 4, по умолчанию).

.PARAMETER Action
    Install — зависимости, Typecheck — типы, Test — тесты, Build — сборка,
    Check — типы и тесты без сборки, All — всё понемногу (по умолчанию).

.EXAMPLE
    .\scripts\windows\frontend.ps1
    Полный цикл по Gateway frontend.

.EXAMPLE
    .\scripts\windows\frontend.ps1 -Client gateway -Action Check
    Быстрая проверка клиента шлюза перед пушем.

.EXAMPLE
    .\scripts\windows\frontend.ps1 -Client storage -Action Build
    Локально собрать страницу хранилища (в образе она собирается сама).
#>
[CmdletBinding()]
param(
    [ValidateSet("gateway", "all")]
    [string]$Client = "all",

    [ValidateSet("Install", "Typecheck", "Test", "Build", "Check", "All")]
    [string]$Action = "All"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$gatewayPath = Join-Path "src" "MARS.ClientUi"

function Invoke-MarsFrontendTool {
    <#
    .SYNOPSIS
        Запускает команду в каталоге фронтенда.
    #>
    param(
        [Parameter(Mandatory)][string]$Directory,
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [hashtable]$Environment = @{},
        [Parameter(Mandatory)][string]$Description
    )

    $previous = @{}

    foreach ($name in $Environment.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name)
        [Environment]::SetEnvironmentVariable($name, $Environment[$name])
    }

    Push-Location $Directory

    try {
        $exit = Invoke-MarsTool -FilePath $FilePath -Arguments $Arguments

        if ($exit -ne 0) {
            Write-Failure "$Description не удалась (код $exit)."
            return $exit
        }

        Write-Success "$Description — ок."
        return 0
    }
    finally {
        Pop-Location

        foreach ($name in $Environment.Keys) {
            [Environment]::SetEnvironmentVariable($name, $previous[$name])
        }
    }
}

function Invoke-MarsGatewayClient {
    <#
    .SYNOPSIS
        Клиент шлюза: Yarn 4 через corepack.
    #>
    param([Parameter(Mandatory)][string]$Name)

    Assert-MarsCommand -Name "corepack" -Hint "Идёт в составе Node 22+. Без него Yarn 4 не включится."

    if ($Action -in @("Install", "All")) {
        $null = Invoke-MarsFrontendTool -Directory $gatewayPath -FilePath "corepack" `
            -Arguments @("enable") -Description "corepack enable"

        $null = Invoke-MarsFrontendTool -Directory $gatewayPath -FilePath "yarn" `
            -Arguments @("install", "--immutable") -Description "yarn install --immutable"
    }

    if ($Action -in @("Typecheck", "Check", "All")) {
        $null = Invoke-MarsFrontendTool -Directory $gatewayPath -FilePath "npx" `
            -Arguments @("tsc", "-b", "--noEmit") -Description "npx tsc -b --noEmit"
    }

    if ($Action -in @("Test", "Check", "All")) {
        $exit = Invoke-MarsFrontendTool -Directory $gatewayPath -FilePath "yarn" `
            -Arguments @("test", "run") `
            -Environment @{ SKIP_STORYBOOK_VITEST = "1" } `
            -Description "yarn test run"

        if ($exit -ne 0) {
            return $exit
        }
    }

    if ($Action -eq "Build") {
        $null = Invoke-MarsFrontendTool -Directory $gatewayPath -FilePath "yarn" `
            -Arguments @("build") -Description "yarn build"
    }

    return 0
}

Push-Location $script:MarsRoot
try {
    Write-Step "Gateway frontend: действие $Action"

    $failures = @()

    if ($Client -in @("gateway", "all")) {
        Write-Info "src/MARS.ClientUi — Yarn 4, клиент оверлеев, админки и сайта"
        $exit = Invoke-MarsGatewayClient -Name "gateway"

        if ($exit -ne 0) { $failures += "gateway" }
    }


    if ($failures.Count -gt 0) {
        Write-Failure "Красные фронтенды: $($failures -join ', ')"
        exit 1
    }

    Write-Success "Фронтенды зелёные."
}
catch {
    # Ошибка печатается через Write-Failure, а не через throw: вызов
    # `powershell -File` из другого powershell оборачивает throw в
    # NativeCommandError, и на экране остаётся стена вывода с CategoryInfo.
    Write-Failure $_.Exception.Message
    exit 1
}
finally {
    Pop-Location
}
