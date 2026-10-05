<#
.SYNOPSIS
    Гейт перед пушем: форматирование, сборка Release, тесты, фронтенд.

.DESCRIPTION
    То, что `AGENTS.md` требует перед `git push`: зелёные
    `dotnet build MARS.slnx -c Release` и `dotnet test MARS.slnx`, плюс
    `dotnet csharpier format .`. Скрипт существует не для красоты: локальный
    прогон — единственный, кто отличает «красный новый тест» от «красного
    сломанного соседнего проекта», а CI отличает только второе.

    Шаги выполняются по очереди и останавливают скрипт на первом падении:
    тесты на несобранном коде дают результат, который нельзя интерпретировать.
    В конце печатается таблица с временем каждого шага — она отвечает на
    вопрос «что именно столько и ждало».

    Скрипт не коммитит и не пушит: это гейт, а не действие над историей.

.PARAMETER Project
    Ограничить тесты подмножеством проектов. Полезно для быстрой итерации, но
    перед пуском гонять надо все.

.PARAMETER Frontend
    Добавить шаг проверки клиента: типы и тесты обоих фронтендов. По умолчанию
    выключено — он медленный, а в CI всё равно гоняется отдельно.

.PARAMETER MaxParallel
    Сколько тестовых проектов гонять одновременно. По умолчанию 1: каждый проект
    с базой поднимает свой контейнер Testcontainers, и параллельный прогон на
    одной машине заканчивается сообщением «Foreground threads were left
    running, forcing process exit» при нуле красных тестов и коде 1. CI так не
    делает: там по одному проекту на задачу.

.EXAMPLE
    .\scripts\windows\verify.ps1
    Полный гейт перед пушем.

.EXAMPLE
    .\scripts\windows\verify.ps1 -Project MARS.Gateway.Tests -MaxParallel 4
    Быстрая итерация: формат, сборка и один проект тестов.

.EXAMPLE
    .\scripts\windows\verify.ps1 -Frontend
    Перед пушем, если менялся клиент.
#>
[CmdletBinding()]
param(
    [string[]]$Project,

    [switch]$Frontend,

    [ValidateRange(1, 32)]
    [int]$MaxParallel = 1,

    [switch]$SkipFormatCheck
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$results = @()

function Invoke-MarsGateStep {
    <#
    .SYNOPSIS
        Выполняет шаг гейта, запоминает время и код возврата.
    .DESCRIPTION
        Шаг, а не `powershell -File` поверх: отдельный процесс вернул бы наружу
        тот же код, но потерял бы вывод и разбор ошибки — а это ровно то, ради
        чего гейт нужен.

        Код берётся из $LASTEXITCODE, а не из факта вызова: вызванный скрипт
        внутри делает нативные команды и сам ничего не бросает, поэтому
        исключение поймает только нативный вызов мимо Invoke-MarsTool.
    #>
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Body
    )

    Write-Step $Name
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $exit = 0
    $global:LASTEXITCODE = 0

    try {
        $null = & $Body

        if ($LASTEXITCODE -ne 0) {
            $exit = $LASTEXITCODE
            Write-Failure "$Name — код $exit"
        }
    }
    catch {
        Write-Failure $_.Exception.Message
        $exit = 1
    }

    $watch.Stop()

    $script:results += [PSCustomObject]@{
        Step     = $Name
        Seconds  = [math]::Round($watch.Elapsed.TotalSeconds, 1)
        ExitCode = $exit
    }

    return $exit
}

function Get-MarsGateWarning {
    <#
    .SYNOPSIS
        Предупреждения о состоянии репозитория перед пушем.
    #>
    $notes = @()

    $branch = (& git rev-parse --abbrev-ref HEAD 2>$null)

    if ($LASTEXITCODE -eq 0 -and $branch -in @("main", "master")) {
        $notes += "Мы на '$branch'. Пушить в main нельзя: работа идёт в отдельной ветке и завершается PR."
    }

    $status = & git status --porcelain

    if ($status) {
        $notes += "В рабочем дереве есть незакоммиченные изменения — гейт проверит не то, что уедет в PR."
    }

    return $notes
}

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "dotnet" -Hint "Нужен .NET SDK версии из global.json (10.0.x)."
    Assert-MarsCommand -Name "git" -Hint "Нужен git: гейт сверяет ветку и состояние дерева."

    Write-Step "Состояние репозитория"

    foreach ($note in (Get-MarsGateWarning)) {
        Write-Note $note
    }

    $formatScript = Join-Path $PSScriptRoot "format.ps1"
    $buildScript = Join-Path $PSScriptRoot "build.ps1"
    $testScript = Join-Path $PSScriptRoot "test.ps1"
    $frontendScript = Join-Path $PSScriptRoot "frontend.ps1"

    # Шаги идут по очереди и каждый следующий имеет смысл только после
    # предыдущего: тесты на красной сборке не проверяют ничего, а фронтенд на
    # непроверенном бэкенде — тоже.
    if (-not $SkipFormatCheck) {
        $formatExit = Invoke-MarsGateStep -Name "Форматирование (CSharpier check)" -Body {
            & $formatScript -Action Check
        }

        if ($formatExit -ne 0) {
            Write-Note "Поправить: .\scripts\windows\format.ps1"
        }
    }

    $buildExit = Invoke-MarsGateStep -Name "Сборка Release" -Body {
        & $buildScript -Action Build
    }

    if ($buildExit -eq 0) {
        $testArguments = @{ MaxParallel = $MaxParallel }

        if ($Project -and $Project.Count -gt 0) {
            $testArguments["Project"] = $Project
        }

        $null = Invoke-MarsGateStep -Name "Тесты" -Body {
            & $testScript @testArguments
        }
    }

    if ($Frontend) {
        $null = Invoke-MarsGateStep -Name "Клиент (типы и тесты)" -Body {
            & $frontendScript -Action Check
        }
    }

    Write-Step "Итог гейта"

    foreach ($row in $results) {
        $color = if ($row.ExitCode -eq 0) { "Green" } else { "Red" }
        $status = if ($row.ExitCode -eq 0) { "зелёный" } else { "КРАСНЫЙ" }

        Write-Host ("    {0,-34} {1,7} с  {2}" -f $row.Step, $row.Seconds, $status) -ForegroundColor $color
    }

    $failed = @($results | Where-Object { $_.ExitCode -ne 0 })

    if ($failed.Count -gt 0) {
        Write-Failure "Гейт не пройден: $(@($failed | ForEach-Object { $_.Step }) -join ', ')"
        exit 1
    }

    Write-Success "Гейт пройден — можно коммитить и пушить в свою ветку."
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
