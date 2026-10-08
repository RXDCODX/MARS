<#
.SYNOPSIS
    Прогон тестов с фильтрами Microsoft.Testing.Platform.

.DESCRIPTION
    Обёртка нужна из-за двух ловушек, обе проверены на этой машине.

    **Фильтры.** `global.json` включает Microsoft.Testing.Platform, а проекты
    на xunit.v3 имеют `OutputType=Exe`. Фильтр VSTest-вида `--filter` молча
    находит ноль тестов и выходит с кодом 8, поэтому опции тест-приложения
    передаются после `--`, а имена параметров здесь именно MTP-овские:
    `--filter-class`, `--filter-method`, `--filter-namespace`, `--filter-trait`.

    **Ноль тестов при зелёной сборке.** На SDK 10.0.401 интеграция `dotnet test`
    с MTP отдавала «Запущено ноль тестов» и код 5, тогда как запуск того же dll
    через `dotnet exec` выполнял все тесты. Если сборка и сам тест-проект
    собираются, а прогон не нашёл ни одного теста — это оно, а не сломанный
    тест. `-UseTestHost` запускает dll напрямую и обходит интеграцию.

    Общая сборка решения выполняется один раз до прогонов, а сами тесты идут
    с `--no-build`: иначе каждый проект собирает решение заново, и время уходит
    не в тесты.

.PARAMETER Project
    Имена тестовых проектов из `tests/`. Не заданы — гоняются все, кроме
    MARS.ClientUi.Tests (ему нужен стенд, см. `e2e.ps1`).

.PARAMETER MaxParallel
    Сколько проектов гонять одновременно. По умолчанию 1, и это не
    осторожность, а разбор: при параллельном прогоне Solution на одной машине
    тестовые процессы завершались с `[FATAL ERROR] Foreground threads were left
    running, forcing process exit` — при нуле красных тестов и коде 1. Причина в
    том, что каждый проект с базой поднимает свой контейнер Testcontainers, и
    при большом параллелизме хост-потоки не успевают завершиться. CI так не
    делает: там по одному проекту на задачу. Параллельный режим полезен на
    быстрых проектах без базы.

.EXAMPLE
    .\scripts\windows\test.ps1 -Project MARS.Gateway.Tests
    Один проект.

.EXAMPLE
    .\scripts\windows\test.ps1 -Project MARS.Shared.Tests -FilterClass "*HealthCheck*"
    Один класс в одном проекте. Именно так, а не `--filter`.

.EXAMPLE
    .\scripts\windows\test.ps1 -Project MARS.TwitchCore.Tests -FilterMethod "*Foo.Bar*"
    Один метод.

.EXAMPLE
    .\scripts\windows\test.ps1 -Project MARS.Admin.Tests -UseTestHost
    Обойти интеграцию dotnet test с MTP, если она находит ноль тестов.

.EXAMPLE
    .\scripts\windows\test.ps1 -MaxParallel 8
    Все проекты, восемь параллельно. Логи — в artifacts/test-logs.
#>
[CmdletBinding()]
param(
    [string[]]$Project,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$NoBuild,

    [switch]$UseTestHost,

    [string]$FilterClass,
    [string]$FilterMethod,
    [string]$FilterNamespace,
    [string]$FilterTrait,

    [ValidateRange(1, 32)]
    [int]$MaxParallel = 1
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

function Get-MarsTestFilterArgument {
    <#
    .SYNOPSIS
        Аргументы MTP-фильтрации: после `--` у dotnet test, без него у dll.
    #>
    $arguments = @()

    if (-not [string]::IsNullOrWhiteSpace($FilterClass)) {
        $arguments += @("--filter-class", $FilterClass)
    }

    if (-not [string]::IsNullOrWhiteSpace($FilterMethod)) {
        $arguments += @("--filter-method", $FilterMethod)
    }

    if (-not [string]::IsNullOrWhiteSpace($FilterNamespace)) {
        $arguments += @("--filter-namespace", $FilterNamespace)
    }

    if (-not [string]::IsNullOrWhiteSpace($FilterTrait)) {
        $arguments += @("--filter-trait", $FilterTrait)
    }

    return $arguments
}

function Invoke-MarsSingleTestProject {
    <#
    .SYNOPSIS
        Один тестовый проект: `dotnet test` либо dll через `dotnet exec`.
    .OUTPUTS
        Код возврата. 5 и 8 дополнительно трактуются вызывающим кодом: это не
        «красный тест», а «тесты не нашлись».
    #>
    param([Parameter(Mandatory)][string]$Name)

    $projectFile = "tests\$Name\$Name.csproj"
    $filter = @(Get-MarsTestFilterArgument)

    if ($UseTestHost) {
        $dll = "tests\$Name\bin\$Configuration\net10.0-windows\$Name.dll"

        if (-not (Test-Path -LiteralPath $dll)) {
            throw "Нет собранного теста: $dll. -UseTestHost требует собранный проект: уберите -NoBuild."
        }

        $arguments = @("exec", $dll) + $filter

        return Invoke-MarsTool -FilePath "dotnet" -Arguments $arguments
    }

    $arguments = @("test", $projectFile, "-c", $Configuration, "--no-build")

    if ($filter.Count -gt 0) {
        $arguments += "--"
        $arguments += $filter
    }

    return Invoke-MarsTool -FilePath "dotnet" -Arguments $arguments
}

function Write-MarsNoTestsHint {
    param([Parameter(Mandatory)][int]$Exit)

    Write-Note "Код $Exit от тест-приложения означает, что тесты не нашлись, а не что они красные."
    Write-Note "Проверьте имя класса и проекта: фильтр чувствителен к регистру."
    Write-Note "Обойти интеграцию dotnet test с MTP: -UseTestHost."
}

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "dotnet" -Hint "Нужен .NET SDK версии из global.json (10.0.x)."

    # Resolve-MarsTestProject требует непустой список: у Mandatory-массива
    # PowerShell считает пустой массив «не переданным» и пытается спросить
    # значение у пользователя, а скрипт не интерактивен.
    $targets = @(
        if ($Project -and $Project.Count -gt 0) { Resolve-MarsTestProject -Name $Project }
        else { Get-MarsTestProject }
    )

    if ($UseTestHost -and $MaxParallel -gt 1) {
        throw (
            "-UseTestHost и -MaxParallel несовместимы: параллельный режим запускает" +
            " `dotnet test`, а хост-режим — dll напрямую через dotnet exec."
        )
    }

    if ($UseTestHost -and -not $NoBuild) {
        $NoBuild = $true
    }

    if (-not $NoBuild) {
        Write-Step "Сборка решения ($Configuration)"
        $buildArguments = @("build", "MARS.slnx", "-c", $Configuration)
        $null = Invoke-MarsToolOrFail `
            -FilePath "dotnet" `
            -Arguments $buildArguments `
            -Description "Сборка решения"
    }

    Write-Step "Тесты: $($targets.Count) проект(ов), конфигурация $Configuration"

    $filterDescription = @(Get-MarsTestFilterArgument) -join " "
    if ($filterDescription) {
        Write-Info "Фильтр: $filterDescription"
    }

    $failed = @()
    $empty = @()
    $logRoot = Join-Path "artifacts" "test-logs"

    if ($MaxParallel -gt 1) {
        # Параллелизм через Start-Process: PowerShell 5.1 не умеет
        # Start-ThreadJob, а `dotnet test` и так уходит в отдельный процесс.
        # Лог каждого проекта пишется на диск — на красном проекте его лог и
        # есть то, что нужно смотреть.
        New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

        Write-Info "Параллельно: $MaxParallel. Логи: $logRoot"

        $queue = [System.Collections.Generic.List[string]]::new()
        foreach ($name in $targets) { $queue.Add($name) }

        $running = @()

        while ($queue.Count -gt 0 -or $running.Count -gt 0) {
            while ($queue.Count -gt 0 -and $running.Count -lt $MaxParallel) {
                $next = $queue[0]
                $queue.RemoveAt(0)

                $log = Join-Path $logRoot "$next.log"
                $arguments = @(
                    "test", "tests\$next\$next.csproj",
                    "-c", $Configuration,
                    "--no-build"
                )

                $filter = @(Get-MarsTestFilterArgument)
                if ($filter.Count -gt 0) {
                    $arguments += "--"
                    $arguments += $filter
                }

                $process = Start-Process -FilePath "dotnet" -ArgumentList $arguments `
                    -NoNewWindow -PassThru `
                    -RedirectStandardOutput $log -RedirectStandardError "$log.err"

                # .Handle читается до старта: PowerShell 5.1 не кэширует
                # дескриптор у Process, отданного Start-Process, и .NET не может
                # опросить код возврата позже.
                $null = $process.Handle

                $running += [PSCustomObject]@{ Name = $next; Process = $process; Log = $log }
                Write-Info "$next — запущены"
            }

            Start-Sleep -Milliseconds 400

            $stillRunning = @()

            foreach ($item in $running) {
                if ($item.Process.HasExited) {
                    if ($item.Process.ExitCode -eq 0) {
                        Write-Success "$($item.Name) — зелёные"
                    }
                    elseif ($item.Process.ExitCode -eq 5 -or $item.Process.ExitCode -eq 8) {
                        Write-Note "$($item.Name) — тесты не нашлись (код $($item.Process.ExitCode)), лог: $($item.Log)"
                        $empty += $item.Name
                    }
                    else {
                        Write-Failure "$($item.Name) — КРАСНЫЕ (код $($item.Process.ExitCode)), лог: $($item.Log)"
                        $failed += $item.Name
                    }
                }
                else {
                    $stillRunning += $item
                }
            }

            $running = $stillRunning
        }
    }
    else {
        foreach ($name in $targets) {
            Write-Info "$name"
            $exit = Invoke-MarsSingleTestProject -Name $name

            if ($exit -eq 0) {
                Write-Success "$name — зелёные"
            }
            elseif ($exit -eq 5 -or $exit -eq 8) {
                Write-MarsNoTestsHint -Exit $exit
                $empty += $name
            }
            else {
                Write-Failure "$name — красные (код $exit)"
                $failed += $name
            }
        }
    }

    Write-Step "Итог"

    if ($empty.Count -gt 0) {
        Write-Note "Тесты не нашлись в: $($empty -join ', ')"
    }

    if ($failed.Count -gt 0) {
        Write-Failure "Красные проекты: $($failed -join ', ')"
        exit 1
    }

    if ($empty.Count -gt 0) {
        exit 1
    }

    Write-Success "Все проекты зелёные: $($targets.Count)."
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
