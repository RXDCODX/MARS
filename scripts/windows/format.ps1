<#
.SYNOPSIS
    Форматирование и проверка форматирования CSharpier.

.DESCRIPTION
    CSharpier — единственный форматтер репозитория, `.csharpierrc` в нём нет,
    то есть действуют умолчания (printWidth 100, 4 пробела). Он лежит в
    локальном манифесте `.config/dotnet-tools.json`, а не в системном наборе,
    поэтому перед запуском скрипт зовёт `dotnet tool restore` — на свежей клоне
    `dotnet csharpier` просто не находится.

    **CLI 1.x — с подкомандами.** `format <path>` пишет, `check <path>` только
    проверяет. Вызов `dotnet csharpier <path>` без подкоманды в 1.x не
    существует (в 0.30.6 было наоборот).

    Версию важно не понижать: на 0.30.6 C# 14 extension members давали «Failed to
    compile so was not formatted» с кодом 1, и автоформат в CI ронялся целиком.

    Проекты в `tests/` помечены `CSharpier_Bypass`: их файлы форматтер не
    трогает, и проверка на них ничего не требует.

.PARAMETER Action
    Format — отформатировать (по умолчанию), Check — только проверить и ничего
    не менять.

.PARAMETER Path
    Что обрабатывать. По умолчанию `.` — весь репозиторий, как в
    `auto-format.yml`.

.EXAMPLE
    .\scripts\windows\format.ps1
    Отформатировать репозиторий. Это обязательный шаг перед пушем: с переходом
    на ветки `auto-format.yml` больше не наезжает на нашу работу.

.EXAMPLE
    .\scripts\windows\format.ps1 -Action Check
    Проверка без правок — для CI-подобного гейта.

.EXAMPLE
    .\scripts\windows\format.ps1 -Path src\MARS.Gateway
    Только один сервис.
#>
[CmdletBinding()]
param(
    [ValidateSet("Format", "Check")]
    [string]$Action = "Format",

    [string]$Path = "."
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "dotnet" -Hint "Нужен .NET SDK версии из global.json (10.0.x)."

    $null = Initialize-MarsDotnetTool

    # Подкоманда строчными буквами: CLI 1.x различает регистр.
    $subcommand = if ($Action -eq "Check") { "check" } else { "format" }

    Write-Step "CSharpier $subcommand $Path"

    $exit = Invoke-MarsToolOrFail `
        -FilePath "dotnet" `
        -Arguments @("csharpier", $subcommand, $Path) `
        -Description "CSharpier $subcommand"

    if ($Action -eq "Check") {
        Write-Success "Форматирование соответствует."
    }
    else {
        Write-Success "Отформатировано. Изменения видны в git status — их нужно закоммитить в ту же задачу."
    }
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
