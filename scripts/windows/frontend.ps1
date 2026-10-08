<#
.SYNOPSIS
    Оба фронтенда репозитория: типы, тесты и сборка.

.DESCRIPTION
    Фронтендов в репозитории два, и инструменты у них разные. Путать их нельзя:
    наборы зависимостей, проверок и ловушек не совпадают.

    |                        | src/MARS.Gateway/ClientApp          | src/MARS.MediaStorage/ClientApp |
    |------------------------|------------------------------------|----------------------------------|
    | Что это                | оверлеи, админка, сайт             | страница хранилища               |
    | Пакетный менеджер      | Yarn 4 (packageManager в manifest) | npm                             |
    | lock-файл              | yarn.lock                          | package-lock.json                |

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

    Сборка `ui-dist` хранилища намеренно не делается по умолчанию: она
    происходит внутри образа, а результат в `ui-dist/` в `.gitignore`.

.PARAMETER Client
    gateway — только клиент шлюза (Yarn), storage — только страница хранилища
    (npm), all — оба (по умолчанию).

.PARAMETER Action
    Install — зависимости, Typecheck — типы, Test — тесты, Build — сборка,
    Check — типы и тесты без сборки, All — всё понемногу (по умолчанию).

.EXAMPLE
    .\scripts\windows\frontend.ps1
    Полный цикл по обоим фронтендам.

.EXAMPLE
    .\scripts\windows\frontend.ps1 -Client gateway -Action Check
    Быстрая проверка клиента шлюза перед пушем.

.EXAMPLE
    .\scripts\windows\frontend.ps1 -Client storage -Action Build
    Локально собрать страницу хранилища (в образе она собирается сама).
#>
[CmdletBinding()]
param(
    [ValidateSet("gateway", "storage", "all")]
    [string]$Client = "all",

    [ValidateSet("Install", "Typecheck", "Test", "Build", "Check", "All")]
    [string]$Action = "All"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$gatewayPath = Join-Path "src" "MARS.Gateway\ClientApp"
$storagePath = Join-Path "src" "MARS.MediaStorage\ClientApp"

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

function Invoke-MarsStorageClient {
    <#
    .SYNOPSIS
        Страница хранилища: npm, не Yarn.
    #>
    param([Parameter(Mandatory)][string]$Name)

    Assert-MarsCommand -Name "npm" -Hint "Node.js 22+ вместе с npm."

    if ($Action -in @("Install", "All")) {
        $null = Invoke-MarsFrontendTool -Directory $storagePath -FilePath "npm" `
            -Arguments @("ci") -Description "npm ci"
    }

    if ($Action -in @("Typecheck", "Check", "All")) {
        $null = Invoke-MarsFrontendTool -Directory $storagePath -FilePath "npm" `
            -Arguments @("run", "typecheck") -Description "npm run typecheck"
    }

    if ($Action -eq "Test") {
        Write-Note "У страницы хранилища нет тестов: в package.json нет test-скрипта. Проверяются типы и сборка."
        $null = Invoke-MarsFrontendTool -Directory $storagePath -FilePath "npm" `
            -Arguments @("run", "typecheck") -Description "npm run typecheck"
    }

    if ($Action -in @("Build", "All")) {
        # ui-dist собирается внутри образа; локальная сборка — только чтобы
        # убедиться, что конфигурация vite и препроцессоров в порядке.
        $null = Invoke-MarsFrontendTool -Directory $storagePath -FilePath "npm" `
            -Arguments @("run", "build") -Description "npm run build"
    }

    return 0
}

Push-Location $script:MarsRoot
try {
    Write-Step "Фронтенд: $Client, действие $Action"

    $failures = @()

    if ($Client -in @("gateway", "all")) {
        Write-Info "src/MARS.Gateway/ClientApp — Yarn 4, клиент оверлеев, админки и сайта"
        $exit = Invoke-MarsGatewayClient -Name "gateway"

        if ($exit -ne 0) { $failures += "gateway" }
    }

    if ($Client -in @("storage", "all")) {
        Write-Info "src/MARS.MediaStorage/ClientApp — npm, страница хранилища"
        $exit = Invoke-MarsStorageClient -Name "storage"

        if ($exit -ne 0) { $failures += "storage" }
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
