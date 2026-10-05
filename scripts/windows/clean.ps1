<#
.SYNOPSIS
    Уборка после прогонов: артефакты сборки и мусор тестовых контейнеров.

.DESCRIPTION
    Две разные вещи, которые выглядят похоже, но требуют разных действий.

    **Артефакты сборки** (`Artifacts`) — `bin`, `obj`, `TestResults`,
    `coverage-local`, `publish`, `dist` и `ui-dist` клиентов, `.vs`. Удаляются
    без вопросов: всё воспроизводимо. `node_modules` и `.yarn` не трогаются без
    `-NodeModules`: их восстановление — это минуты и сеть.

    **Мусор тестовых контейнеров** (`Docker`) — то, что `PostgresTestDbContextFactory`
    и `MarsPostgres` должны были убрать сами. Ryuk (resource reaper) — страховка,
    а не механизм удаления, и полагаться на него нельзя: на этой машине он
    остался в состоянии `Created` и не отработал ни разу. Поэтому скрипт сначала
    **показывает**, что осталось, и удаляет только по `-Remove`.

    Про тома сказано отдельно, потому что это ловушка: образ `postgres:16`
    объявляет `VOLUME /var/lib/postgresql/data`, и этот том создаёт демон **до**
    контейнера. Меток контейнера в нём нет, Ryuk такой том не видит, и
    `docker rm` его не убирает. Testcontainers обходит это через tmpfs
    (`WithTmpfsMount`), но если монтирования не было, том уже создан, и
    `docker volume ls -f dangling=true` его покажет. Удалять чужие висящие тома
    скрипт не будет: на этой машине их уже около сорока, и почти все — не наши.

.PARAMETER Action
    Artifacts — убрать артефакты сборки (по умолчанию), Docker — показать
    оставшиеся тестовые контейнеры, All — и то и другое.

.PARAMETER Remove
    Для Docker: удалить найденные контейнеры с метками testcontainers, а не
    только показать их.

.PARAMETER NodeModules
    Для Artifacts: удалить `node_modules` обоих клиентов.

.EXAMPLE
    .\scripts\windows\clean.ps1
    Убрать артефакты сборки.

.EXAMPLE
    .\scripts\windows\clean.ps1 -Action Docker
    Показать, что после тестов осталось в Docker.

.EXAMPLE
    .\scripts\windows\clean.ps1 -Action All -Remove
    Артефакты плюс контейнеры, оставшиеся после прогонов.
#>
[CmdletBinding()]
param(
    [ValidateSet("Artifacts", "Docker", "All")]
    [string]$Action = "Artifacts",

    [switch]$Remove,

    [switch]$NodeModules
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

function Remove-MarsDirectory {
    <#
    .SYNOPSIS
        Удаляет каталог и печатает, что именно.
    #>
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Remove-Item -LiteralPath $Path -Recurse -Force
    Write-Info "удалён $Path"
}

function Invoke-MarsCleanArtifacts {
    # Список относительных путей. Каталоги в .gitignore убираются все до
    # одного, кроме node_modules: он тоже в .gitignore, но восстанавливается
    # только сетью.
    $targets = @(
        "TestResults",
        "coverage-local",
        "coverage-report",
        "publish",
        "artifacts",
        ".vs",
        "src\MARS.Gateway\ClientApp\dist",
        "src\MARS.Gateway\ClientApp\storybook-static",
        "src\MARS.Gateway\ClientApp\coverage",
        "src\MARS.MediaStorage\ui-dist"
    )

    Write-Step "Артефакты сборки"

    foreach ($target in $targets) {
        Remove-MarsDirectory -Path $target
    }

    # bin и obj — во всех проектах src/ и tests/.
    $projectDirs = @(Get-ChildItem -LiteralPath "src" -Directory) + @(Get-ChildItem -LiteralPath "tests" -Directory)

    foreach ($dir in $projectDirs) {
        Remove-MarsDirectory -Path (Join-Path $dir.FullName "bin")
        Remove-MarsDirectory -Path (Join-Path $dir.FullName "obj")
    }

    # tsbuildinfo разбросан по клиентам: путь кэша tsc абсолютный и машинный.
    # Обход через Get-MarsSearchFile, а не Get-ChildItem -Recurse: тот заходит в
    # node_modules обоих клиентов и ищет там файлы, которых там нет.
    foreach ($file in (Get-MarsSearchFile -Root "src" -Include @("*.tsbuildinfo"))) {
        Write-Info "удалён $($file.FullName)"
        Remove-Item -LiteralPath $file.FullName -Force
    }

    if ($NodeModules) {
        Remove-MarsDirectory -Path "src\MARS.Gateway\ClientApp\node_modules"
        Remove-MarsDirectory -Path "src\MARS.MediaStorage\ClientApp\node_modules"
    }
    else {
        Write-Note "node_modules оставлены. Убрать: .\scripts\windows\clean.ps1 -NodeModules"
    }

    Write-Success "Артефакты убраны. Следующая сборка будет полной."
}

function Invoke-MarsCleanDocker {
    Write-Step "Контейнеры тестов (метка org.testcontainers)"

    $containers = @(
        & docker ps -a --filter "label=org.testcontainers" `
            --format "{{.Names}}`t{{.Status}}`t{{.Image}}" 2>$null
    )

    if ($LASTEXITCODE -ne 0) {
        throw "docker не отвечает. Проверьте, что Docker Desktop запущен."
    }

    if ($containers.Count -eq 0) {
        Write-Success "Контейнеров с метками testcontainers нет — уборка тестов отработала."
    }
    elseif ($Remove) {
        foreach ($container in $containers) {
            $name = ($container -split "`t")[0]

            # -f, а не Stop: удаление обязано быть синхронным (DeleteAsync), а
            # остановленный контейнер остаётся в docker ps -a и держит имя.
            & docker rm -f $name | Out-Null
            Write-Info "удалён $name"
        }

        Write-Success "Контейнеры удалены."
    }
    else {
        foreach ($container in $containers) {
            Write-Note $container
        }

        Write-Note "Удалить: .\scripts\windows\clean.ps1 -Action Docker -Remove"
    }

    Write-Step "Висящие тома"

    $dangling = @(& docker volume ls -f dangling=true -q 2>$null)

    if ($dangling.Count -eq 0) {
        Write-Success "Висящих томов нет."
    }
    else {
        Write-Note "Висящих томов: $($dangling.Count). Удалять их скрипт не будет: на этой машине их много, и почти все — не наши."
        Write-Note "Свои тома стенда: docker volume ls | Select-String mars"
    }
}

Push-Location $script:MarsRoot
try {
    if ($Action -in @("Artifacts", "All")) {
        Invoke-MarsCleanArtifacts
    }

    if ($Action -in @("Docker", "All")) {
        Invoke-MarsCleanDocker
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
