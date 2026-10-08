<#
.SYNOPSIS
    Миграции EF Core: добавить, убрать, обновить базу, посмотреть список.

.DESCRIPTION
    Фабрика проекта времени разработки лежит в `src/MARS.X/Data/DesignTime`,
    поэтому `dotnet ef` работает без запущенного стенда и без строк подключения
    в окружении. Проект без такой фабрики скрипт не примет: там `dotnet ef`
    попытался бы поднять хост приложения, то есть пошёл бы в сеть и в базу.

    `dotnet-ef` — **глобальный** инструмент, его нет в
    `.config/dotnet-tools.json` (в манифесте лежат только `csharpier` и
    `reportgenerator`). Проверка версии стоит первым шагом, иначе первая же
    команда падает с «не удаётся выполнить», то есть без указания, что доставить.

    Миграции применяются сервисом сами при старте — синхронно и до `app.Run()`
    (`RunMarsSchemaMigrationsAsync`), иначе фоновые службы успевают обратиться к
    несуществующим таблицам (42P01). Поэтому `Update` нужен только чтобы
    проверить миграцию на живой базе, а не чтобы подготовить стенд.

    Требуется Docker: тесты базы поднимают Testcontainers, а `Update` ходит в
    настоящий PostgreSQL, как и на стенде.

.PARAMETER Action
    Add — создать миграцию, Remove — убрать последнюю, Update — применить к базе,
    Script — печатать SQL, не выполняя его, List — список уже созданных.

.PARAMETER Project
    Сервис из `src/` (например, `MARS.TwitchCore`) либо путь к .csproj.

.EXAMPLE
    .\scripts\windows\migrate.ps1 -Project MARS.TwitchCore -Action Add -Name AddRewardHistory
    Новая миграция для TwitchCore.

.EXAMPLE
    .\scripts\windows\migrate.ps1 -Project MARS.MediaStorage -Action List
    Что уже нагенерировано.

.EXAMPLE
    .\scripts\windows\migrate.ps1 -Project MARS.CinemaQueue -Action Script -Name Initial
    SQL миграции без её применения.
#>
[CmdletBinding()]
param(
    [ValidateSet("Add", "Remove", "Update", "Script", "List")]
    [string]$Action = "List",

    [Parameter(Mandatory)]
    [string]$Project,

    # Имя миграции. Обязательно для Add, необязательно для Script (там это
    # миграция, с которой начинается скрипт).
    [string]$Name
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "dotnet" -Hint "Нужен .NET SDK версии из global.json (10.0.x)."

    Write-Step "Миграции: $Action, проект $Project"

    # dotnet-ef глобальный: `dotnet ef` без него просто не существует.
    $probe = Invoke-MarsTool -FilePath "dotnet" -Arguments @("ef", "--version")

    if ($probe -ne 0) {
        throw (
            "dotnet-ef не установлен. Он глобальный и в .config/dotnet-tools.json его нет:" +
            [Environment]::NewLine + "    dotnet tool install --global dotnet-ef"
        )
    }

    Write-Info "dotnet-ef: $(& dotnet ef --version 2>&1 | Select-Object -Last 1)"

    $projectFile = Resolve-MarsProjectFile -Name $Project
    $projectDirectory = Split-Path -Parent $projectFile
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile)

    # Фабрика времени разработки — условие работоспособности `dotnet ef`.
    # Без неё EF поднимает Program.cs, то есть ходит в сеть и в настоящие
    # сервисы, и падение выглядит совсем не как «нет фабрики».
    $factory = @(Get-ChildItem -LiteralPath $projectDirectory -Recurse -File -Filter "*DbContextFactory.cs")

    if ($factory.Count -eq 0) {
        throw (
            "В $projectName нет Data/DesignTime/*DbContextFactory.cs, и dotnet ef будет" +
            " поднимать приложение целиком. Список сервисов с фабриками:" +
            [Environment]::NewLine + "    " +
            (@(Get-ChildItem -LiteralPath "src" -Recurse -File -Filter "*DbContextFactory.cs") |
                ForEach-Object { Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $_.DirectoryName)) } |
                Sort-Object -Unique) -join ", "
        )
    }

    $arguments = @("ef", "migrations")

    if ($Action -in @("Add", "Script") -and [string]::IsNullOrWhiteSpace($Name)) {
        throw "Действию $Action нужно -Name: имя новой миграции либо миграция, с которой начинается скрипт."
    }

    switch ($Action) {
        "List" { $arguments += @("list") }
        "Remove" { $arguments += @("remove", "--project", $projectFile) }
        "Update" { $arguments += @("update", "--project", $projectFile) }
        "Script" { $arguments += @("script", $Name, "--project", $projectFile) }
        "Add" { $arguments += @("add", $Name, "--project", $projectFile) }
    }

    if ($Action -eq "Update") {
        Write-Note "Update применит миграции к базе из строки подключения сервиса. Требуется поднятый postgres."
    }

    $null = Invoke-MarsToolOrFail `
        -FilePath "dotnet" `
        -Arguments $arguments `
        -Description "dotnet ef migrations $($Action.ToLowerInvariant())"

    Write-Success "Готово."
    Write-Info "Новые файлы миграций лежат в src/$projectName/Migrations — их нужно закоммитить в ту же задачу."
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
