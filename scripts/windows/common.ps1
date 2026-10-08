<#
.SYNOPSIS
    Общие помощники скриптов scripts/windows.

.DESCRIPTION
    Файл только подключается (dot-source), выполнения на верхнем уровне у него
    нет: все команды здесь — функции плюс вычисление корня репозитория.

    Общее у всех скриптов одно: корень ищется вверх по дереву от каталога
    скрипта, а не берётся из текущего каталога. Скрипты зовут и из корня, и из
    IDE, и из произвольного места, а путь, зашитый в скрипт, сломался бы при
    первом же клоне у другого человека.

    Ещё одно: скрипты опираются на PowerShell 5.1 — версию, которая стоит в
    Windows из коробки и в Windows Server 2016/2019, где PowerShell 7 может и не
    быть. Поэтому здесь нет тернарных выражений `? :`, нет `??`, нет
    `ConvertFrom-Json -AsHashtable` и нет `Join-String`.

.NOTES
    Источник правды по составу репозитория — сам репозиторий: тестовые проекты
    берутся из каталога `tests/`, образы для публикации — из матрицы
    `release-microservices.yml`. Второй список в скрипте разошёлся бы с первым
    при первом же добавлении сервиса, и расхождение было бы молчаливым.
#>

Set-StrictMode -Version Latest

# --- Корень репозитория ----------------------------------------------------

function Find-MarsRepositoryRoot {
    <#
    .SYNOPSIS
        Ищет корень репозитория вверх по дереву от каталога скрипта.
    #>
    $directory = Get-Item -LiteralPath $PSScriptRoot

    while ($null -ne $directory) {
        if (Test-Path -LiteralPath (Join-Path $directory.FullName "MARS.slnx")) {
            return $directory.FullName
        }

        $directory = $directory.Parent
    }

    throw (
        "Корень репозитория не найден вверх по дереву от $PSScriptRoot." +
        " Ожидался файл MARS.slnx — скрипт лежит вне дерева исходников."
    )
}

$script:MarsRoot = Find-MarsRepositoryRoot

# --- Вывод -----------------------------------------------------------------

function Write-Step {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Info {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "    $Message" -ForegroundColor DarkGray
}

function Write-Success {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "    $Message" -ForegroundColor Green
}

function Write-Note {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "    ! $Message" -ForegroundColor Yellow
}

function Write-Failure {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "    x $Message" -ForegroundColor Red
}

# --- Внешние команды -------------------------------------------------------

function Assert-MarsCommand {
    <#
    .SYNOPSIS
        Проверяет, что команда есть в PATH, и объясняет, что доставить.
    #>
    param(
        [Parameter(Mandatory)][string]$Name,
        [string]$Hint
    )

    if ($null -ne (Get-Command -Name $Name -CommandType Application -ErrorAction SilentlyContinue)) {
        return
    }

    $message = "Команда '$Name' не найдена в PATH."
    if ($Hint) {
        $message = "$message $Hint"
    }

    throw $message
}

function Invoke-MarsTool {
    <#
    .SYNOPSIS
        Запускает внешнюю программу, печатает её вывод и возвращает код возврата.
    .DESCRIPTION
        Обёртка нужна из-за PowerShell 5.1: при $ErrorActionPreference = "Stop"
        любой вывод нативной программы в stderr превращается в terminating
        NativeCommandError. `dotnet ef` и `coverage-gate.py` пишут в stderr
        осмысленные сообщения, и скрипт падал бы на них раньше, чем показал
        причину, — то есть ровно там, где его помощь нужнее всего.
    #>
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @()
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    try {
        & $FilePath @Arguments 2>&1 | ForEach-Object { Write-Host $_ }
        return $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Invoke-MarsToolOrFail {
    <#
    .SYNOPSIS
        Запускает внешнюю программу и обрывает скрипт на ненулевом коде.
    #>
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [Parameter(Mandatory)][string]$Description
    )

    $exit = Invoke-MarsTool -FilePath $FilePath -Arguments $Arguments

    if ($exit -ne 0) {
        throw "$Description не удалась (код $exit)."
    }

    return $exit
}

# --- Состав репозитория ----------------------------------------------------

function Get-MarsTestProject {
    <#
    .SYNOPSIS
        Имена тестовых проектов из tests/, у которых есть .csproj.
    .DESCRIPTION
        Источник правды — каталог `tests/`, а не матрица `ci.yml`: проект,
        забытый в матрице, выпал бы и из списка, и это было бы не видно.

        MARS.ClientUi.Tests исключается: это навигационные тесты клиента на
        Playwright, им нужен поднятый стенд и образ с браузерами. Локально они
        гоняются через `e2e.ps1`, в матрицу `tests` в ci.yml они тоже не входят.
    #>
    $root = Join-Path $script:MarsRoot "tests"
    $excluded = @("MARS.ClientUi.Tests")

    $projects = @(
        Get-ChildItem -LiteralPath $root -Directory |
            Where-Object { $_.Name.EndsWith(".Tests") } |
            Where-Object { $_.Name -notin $excluded } |
            Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName ($_.Name + ".csproj")) } |
            ForEach-Object { $_.Name } |
            Sort-Object
    )

    if ($projects.Count -eq 0) {
        throw "В tests/ не найдено ни одного тестового проекта. Проверьте корень: $root"
    }

    return $projects
}

function Resolve-MarsTestProject {
    <#
    .SYNOPSIS
        Проверяет, что запрошенные тестовые проекты существуют, и возвращает их.
    #>
    param([Parameter(Mandatory)][string[]]$Name)

    $available = Get-MarsTestProject

    foreach ($item in $Name) {
        if ($available -notcontains $item) {
            throw (
                "Нет такого тестового проекта: $item. Доступны: $($available -join ', ')" +
                " (MARS.ClientUi.Tests гоняет e2e.ps1 — ему нужен стенд)."
            )
        }
    }

    return @($Name)
}

function Resolve-MarsProjectFile {
    <#
    .SYNOPSIS
        Ищет .csproj по имени каталога в src/ и tests/.
    .DESCRIPTION
        Службы в src/ и тесты в tests/ называются одинаково (`MARS.Gateway` и
        `MARS.Gateway.Tests`), поэтому достаточно отброшенных `.Tests` и
        совпадения по имени каталога. Путь принимается и явно — на случай
        нестандартного расположения.
    #>
    param([Parameter(Mandatory)][string]$Name)

    if ($Name.EndsWith(".csproj")) {
        $explicit = if ([System.IO.Path]::IsPathRooted($Name)) {
            $Name
        }
        else {
            Join-Path $script:MarsRoot $Name
        }

        if (-not (Test-Path -LiteralPath $explicit)) {
            throw "Файл проекта не найден: $explicit"
        }

        return (Resolve-Path -LiteralPath $explicit).Path
    }

    $candidate = Join-Path $script:MarsRoot "src\$Name\$Name.csproj"

    if (-not (Test-Path -LiteralPath $candidate)) {
        $testCandidate = Join-Path $script:MarsRoot "tests\$Name\$Name.csproj"

        if (Test-Path -LiteralPath $testCandidate) {
            return (Resolve-Path -LiteralPath $testCandidate).Path
        }

        throw (
            "Проект '$Name' не найден. Ожидался src\$Name\$Name.csproj." +
            " Имя указывается без пути и без .csproj."
        )
    }

    return (Resolve-Path -LiteralPath $candidate).Path
}

function Get-MarsReleaseService {
    <#
    .SYNOPSIS
        Матрица публикации образов, прочитанная из release-microservices.yml.
    .DESCRIPTION
        Список намеренно не зашит в скрипт. Матрица workflow — единственное
        место, где решено, какие образы публикуются: `videos365` в compose
        есть, а в публикации его нет, и зашитый в скрипт список однажды
        опубликовал бы лишний образ или, что хуже, промолчал бы про забытый.
    #>
    $workflow = Join-Path $script:MarsRoot ".github\workflows\release-microservices.yml"

    if (-not (Test-Path -LiteralPath $workflow)) {
        throw "Не найден workflow публикации: $workflow"
    }

    $services = @()
    $current = $null

    foreach ($line in (Get-Content -LiteralPath $workflow)) {
        if ($line -match '^\s*-\s*service:\s*(?<value>\S+)\s*$') {
            if ($null -ne $current) {
                $services += New-MarsReleaseServiceObject -Row $current
            }

            $current = [ordered]@{
                Service    = $Matches["value"]
                Project    = ""
                Dockerfile = "Dockerfile"
            }
        }
        elseif ($null -ne $current -and $line -match '^\s+project:\s*(?<value>\S+)\s*$') {
            $current["Project"] = $Matches["value"]
        }
        elseif ($null -ne $current -and $line -match '^\s+dockerfile:\s*(?<value>\S+)\s*$') {
            $current["Dockerfile"] = $Matches["value"]
        }
    }

    if ($null -ne $current) {
        $services += New-MarsReleaseServiceObject -Row $current
    }

    if ($services.Count -eq 0) {
        throw "В release-microservices.yml не найдено ни одной строки матрицы publish."
    }

    return $services
}

function New-MarsReleaseServiceObject {
    param([Parameter(Mandatory)]$Row)

    if ([string]::IsNullOrWhiteSpace($Row["Project"])) {
        throw "Строка матрицы '$($Row['Service'])' не объявляет project."
    }

    return [PSCustomObject]@{
        Service    = $Row["Service"]
        Project    = $Row["Project"]
        Dockerfile = $Row["Dockerfile"]
        Context    = "."
        DockerfilePath = "src/$($Row['Project'])/$($Row['Dockerfile'])"
    }
}

function Select-MarsReleaseService {
    <#
    .SYNOPSIS
        Отбирает строки матрицы публикации по имени сервиса.
    .DESCRIPTION
        Пустой список означает «все сервисы»: полный набор образов собирается
        одной командой, а перечислять 15 имён вручную — источник опечаток.
    #>
    param([string[]]$Service)

    $all = @(Get-MarsReleaseService)
    $names = @($all | ForEach-Object { $_.Service })

    if (-not $Service -or $Service.Count -eq 0) {
        return $all
    }

    $unknown = @($Service | Where-Object { $names -notcontains $_ })

    if ($unknown) {
        throw (
            "В матрице публикации нет сервиса: $($unknown -join ', ')." +
            " Доступны: $($names -join ', ')"
        )
    }

    return @($all | Where-Object { $_.Service -in $Service })
}

function Show-MarsReleaseService {
    <#
    .SYNOPSIS
        Печатает матрицу публикации: имя образа, каталог с Dockerfile, сам Dockerfile.
    #>
    foreach ($item in (Get-MarsReleaseService)) {
        Write-Info ("{0,-16} {1,-24} {2}" -f $item.Service, $item.Project, $item.Dockerfile)
    }
}

# --- Поиск по файлам -------------------------------------------------------

# Каталоги, которые не обходятся никогда: там лежат копии исходников и
# артефакты, а совпадение в них ничего не значит. Список вынесен в переменную,
# а не в регулярку на совпадениях, потому что обход дороже проверки: с
# node_modules и .git репозиторий проходится минутами.
$script:MarsSkipDirectory = @(
    "obj", "bin", "node_modules", ".git", ".opencode", ".mimocode", ".vs",
    ".vscode", ".idea", ".yarn", "TestResults", "coverage-local",
    "coverage-report", "graphify-out", "dist", "ui-dist", "secrets", "publish"
)

function Test-MarsFilePattern {
    <#
    .SYNOPSIS
        Совпадает ли имя файла хотя бы с одним шаблоном (`*.cs`, `Dockerfile`).
    #>
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$Pattern
    )

    foreach ($item in $Pattern) {
        if ($Name -like $item) {
            return $true
        }
    }

    return $false
}

function Get-MarsSearchFile {
    <#
    .SYNOPSIS
        Файлы дерева подходящих каталогов и расширений.
    .DESCRIPTION
        Обход свой, а не `Get-ChildItem -Recurse`: у того нет исключения
        каталогов из рекурсии, `-Exclude` отфильтровывает найденное, а не
        не спускается. То есть весь node_modules и .git обходятся и замедляют
        каждый вызов в разы.

        Точки соединения и симлинки пропускаются: рекурсия по ним зацикливается
        на Windows (`C:\Users\...\AppData\Local` внутри дерева узла), и поиск
        не заканчивается никогда.
    #>
    param(
        [string]$Root = ".",
        [string[]]$Include = @("*")
    )

    $files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    $pending = [System.Collections.Generic.Stack[string]]::new()
    $pending.Push((Get-Item -LiteralPath $Root).FullName)

    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()

        # Каталог мог исчезнуть между обходом и чтением — это не повод падать.
        $entries = @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction SilentlyContinue)

        foreach ($entry in $entries) {
            if ($entry.PSIsContainer) {
                if ($script:MarsSkipDirectory -contains $entry.Name) {
                    continue
                }

                if (($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    continue
                }

                $pending.Push($entry.FullName)
                continue
            }

            if (Test-MarsFilePattern -Name $entry.Name -Pattern $Include) {
                $files.Add($entry)
            }
        }
    }

    return $files
}

function Get-MarsComposeArgument {
    <#
    .SYNOPSIS
        Аргументы `docker compose --env-file … -f …` для прод- или dev-стенда.
    .DESCRIPTION
        Файл окружения выбирается тем же переключателем `-Dev`, что и compose-файл,
        и это не совпадение: в `docker-compose.dev.yml` стоит
        ASPNETCORE_ENVIRONMENT=Development (сервисы читают appsettings.Development.json),
        а в `docker-compose.yml` — Production. Секреты среды обязаны соответствовать
        той же среде, поэтому отдельного переключателя профиля здесь нет: разъехаться
        могут только два переключателя подряд, а их один.

        `--env-file` обязателен: compose сам читает дефолтный `.env`, которого в
        репозитории больше нет, молча взял бы значения по умолчанию из `${ПЕРЕМЕННАЯ:-}`
        и поднял стенд на пустых паролях.

        `docker-compose.dev.yml` подключается только явно и вторым файлом:
        в `.gitignore` он назван не `override`, поэтому обычный
        `docker compose up` hot-reload не подхватывает молча.
    #>
    param([switch]$Dev)

    $environment = if ($Dev) { ".env.development" } else { ".env.production" }
    $files = @("--env-file", $environment, "-f", "docker-compose.yml")

    if ($Dev) {
        $files += @("-f", "docker-compose.dev.yml")
    }

    return $files
}

function Initialize-MarsEnvironmentFile {
    <#
    .SYNOPSIS
        Создаёт файл окружения (`.env.development` или `.env.production`) из его
        шаблона, если его ещё нет.
    .DESCRIPTION
        Файлы окружений в git не попадают, а compose получает нужный флагом
        `--env-file`. Без этого шага стенд поднимается с пустыми паролями, а
        `01-databases.sh` останавливается на первой проверке — и падает не с
        «нет пароля», а с невнятным сообщением Postgres.

        Существующий файл не перезаписывается: в нём настоящие секреты стенда, а
        шаблон содержит пустые значения. Шаблон копируется только в отсутствие
        файла, и то по шаблону той среды, которую запускают.
    <para>
        Откат на .env.example — не украшение, а страховка на время перехода: пока
        шаблонов окружений в репозитории нет, единственный источник значений для
        стенда это .env.example, и падать из-за его отсутствия было бы хуже, чем
        создать файл из него с предупреждением.
    </para>
    #>
    param([switch]$Dev)

    $environment = if ($Dev) { ".env.development" } else { ".env.production" }
    $target = Join-Path $script:MarsRoot $environment
    $template = Join-Path $script:MarsRoot "$environment.example"

    if (Test-Path -LiteralPath $target) {
        Write-Info "$environment уже есть — не трогаю."
        return $target
    }

    $isFallback = $false

    if (-not (Test-Path -LiteralPath $template)) {
        $legacy = Join-Path $script:MarsRoot ".env.example"

        if (-not (Test-Path -LiteralPath $legacy)) {
            # ${environment}, а не $environment: PowerShell читает `$environment:`
            # как переменную с именем диска и падает на парсинге, до первой строки
            # работы скрипта.
            throw "Нет шаблона ${environment}.example и .env.example, из которого можно создать ${environment}: $template"
        }

        $template = $legacy
        $isFallback = $true
    }

    Copy-Item -LiteralPath $template -Destination $target

    if ($isFallback) {
        Write-Note "$environment создан из .env.example: шаблонов окружений пока нет в репозитории."
        Write-Note "Значения в нём стендовые. Для боевого стенда секреты заполняются вручную."
    }
    else {
        Write-Success "Создан $environment из шаблона. Значения в нём — стендовые, секреты в нём пустые."
    }

    return $target
}

function Initialize-MarsDotnetTool {
    <#
    .SYNOPSIS
        Восстанавливает локальные инструменты репозитория.
    .DESCRIPTION
        `reportgenerator` и `csharpier` лежат в `.config/dotnet-tools.json`, а не
        в системном наборе. На свежей клоне `dotnet csharpier` не найдётся, и
        скрипт падал бы на «инструмент не установлен» вместо сути дела.
    #>
    return Invoke-MarsToolOrFail `
        -FilePath "dotnet" `
        -Arguments @("tool", "restore") `
        -Description "Восстановление локальных инструментов (dotnet tool restore)"
}

function Get-MarsPythonCommand {
    <#
    .SYNOPSIS
        Команда Python: `python` на Windows, `python3` на linux-образе CI.
    #>
    foreach ($candidate in @("python", "python3")) {
        if ($null -ne (Get-Command -Name $candidate -CommandType Application -ErrorAction SilentlyContinue)) {
            return $candidate
        }
    }

    throw (
        "Не найден ни python, ни python3. Ими запускаются .github/scripts/coverage-gate.py" +
        " и coverage-gaps.py — гейт покрытия без них не проверяется."
    )
}

function Get-MarsGitRemote {
    <#
    .SYNOPSIS
        Владелец репозитория из origin, для имени образа вида ghcr.io/<owner>/mars-<service>.
    #>
    $url = (& git remote get-url origin 2>$null)

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($url)) {
        return ""
    }

    # Поддерживаются обе формы: https://github.com/<owner>/<repo> и
    # git@github.com:<owner>/<repo>.git. Регистр владельца в GHCR не важен —
    # сам workflow приводит его к нижнему.
    $match = [regex]::Match($url, '[:/](?<owner>[^/:]+)/[^/]+?(?:\.git)?$')

    if (-not $match.Success) {
        return ""
    }

    return $match.Groups["owner"].Value.ToLowerInvariant()
}