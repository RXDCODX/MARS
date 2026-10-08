<#
.SYNOPSIS
    Сборка решения, restore и publish — теми же командами, что и в CI.

.DESCRIPTION
    Точное соответствие обязательно: `dotnet build MARS.slnx -c Release` из
    `ci.yml` и локальный `dotnet build -c Debug` проверяют разное. В частности,
    предупреждения компиляции здесь становятся ошибками только в Release:
    `TreatWarningsAsErrors` задан в каждом .csproj, а `Debug`-сборка CI не
    проверяет.

.PARAMETER Action
    Restore — только восстановление пакетов, Build — сборка (по умолчанию),
    Publish — публикация одного проекта в publish/<сервис>.

.PARAMETER Project
    Имя каталога из `src/` (например, `MARS.Gateway`) или путь к .csproj.
    Для Restore и Build не задан — собирается всё решение. Для Publish
    обязателен: публиковать решение целиком нечего.

.EXAMPLE
    .\scripts\windows\build.ps1
    Собрать всё решение в Release — то, что требуется перед пушем.

.EXAMPLE
    .\scripts\windows\build.ps1 -Project MARS.Gateway -Configuration Debug
    Быстрая итерация над одним сервисом.

.EXAMPLE
    .\scripts\windows\build.ps1 -Action Publish -Project MARS.Gateway
    Опубликовать сервис в publish/MARS.Gateway (каталог в .gitignore).
#>
[CmdletBinding()]
param(
    [ValidateSet("Restore", "Build", "Publish")]
    [string]$Action = "Build",

    [string]$Project,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "dotnet" -Hint "Нужен .NET SDK версии из global.json (10.0.x)."

    if ($Action -eq "Publish" -and [string]::IsNullOrWhiteSpace($Project)) {
        throw "Для Publish нужен -Project: публиковать решение целиком нечего."
    }

    Write-Step "Действие: $Action, конфигурация: $Configuration"

    if ($Action -eq "Restore") {
        $null = Invoke-MarsToolOrFail `
            -FilePath "dotnet" `
            -Arguments @("restore", "MARS.slnx") `
            -Description "Восстановление пакетов"

        return
    }

    if ($Action -eq "Build") {
        # Отдельный restore перед сборкой нужен не для красоты: без него
        # `dotnet build` сам запускает restore, но неявно, и его ошибка
        # выглядит как ошибка компиляции — а это разные диагностики.
        if (-not $NoRestore) {
            Write-Info "Восстановление пакетов"
            $null = Invoke-MarsToolOrFail `
                -FilePath "dotnet" `
                -Arguments @("restore", "MARS.slnx") `
                -Description "Восстановление пакетов"
        }

        Write-Info "Сборка MARS.slnx"
        $arguments = @("build", "MARS.slnx", "-c", $Configuration)

        if ($NoRestore) {
            $arguments += "--no-restore"
        }

        $null = Invoke-MarsToolOrFail `
            -FilePath "dotnet" `
            -Arguments $arguments `
            -Description "Сборка решения"

        Write-Success "Решение собрано."
        return
    }

    # Publish
    $projectFile = Resolve-MarsProjectFile -Name $Project
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile)
    $output = Join-Path "publish" $projectName

    $arguments = @("publish", $projectFile, "-c", $Configuration, "-o", $output)

    if ($NoRestore) {
        $arguments += "--no-restore"
    }

    Write-Info "Публикация $projectName в $output"
    $null = Invoke-MarsToolOrFail `
        -FilePath "dotnet" `
        -Arguments $arguments `
        -Description "Публикация $projectName"

    Write-Success "Опубликовано в $output (каталог publish/ в .gitignore)."
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
