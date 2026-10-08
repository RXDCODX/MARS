<#
.SYNOPSIS
    Образы релиза: собрать, запушить в registry, поставить тег версии.

.DESCRIPTION
    Локальный аналог `release-microservices.yml`. Список образов не зашит в
    скрипт, а читается из матрицы `publish` того же workflow: единственное
    место, где решено, что публикуется. Зашитый список однажды разошёлся бы с
    матрицей — и молча, как это уже было со сводкой релиза, где `client-ui` и
    `shikimori` публиковались, но в итог не попадали.

    Три действия:

    - `List` — показать матрицу: имя образа, каталог, Dockerfile;
    - `Images` — собрать образы (по умолчанию все). Без `-Push` образы
      тегируются как `mars-<service>` — так compose находит их локально; с
      `-Push` имя становится `<registry>/<owner>/mars-<service>`, как в
      workflow;
    - `Tag` — поставить аннотированный git-тег `vX.Y.Z` и запушить его.
      Именно пуш тега `v*` запускает публикацию образов в CI, поэтому
      публикация руками и релизный тег — разные вещи, и скрипт их не смешивает.

    Набор тегов повторяет workflow: релизный тег, всегда `sha-<короткий хеш>`,
    и `latest` — только для семантической версии (`^v[0-9]+(\.[0-9]+)*$`).
    Иначе `v1.0.0-rc1` стал бы прод-образом под именем `latest`.

    Финальный stage Dockerfile — `final`: он содержит curl, который нужен
    compose-healthcheck'ам (в `mcr.microsoft.com/dotnet/aspnet` его нет, иначе
    healthcheck даёт exit code 127 → unhealthy).

.PARAMETER Service
    Имена сервисов из матрицы публикации. Не заданы — все.

.EXAMPLE
    .\scripts\windows\release.ps1 -Action List
    Что вообще публикуется.

.EXAMPLE
    .\scripts\windows\release.ps1 -Action Images -Service gateway -Tag v1.2.3
    Собрать образ шлюза локально под релизным тегом.

.EXAMPLE
    .\scripts\windows\release.ps1 -Action Images -Service gateway -Tag v1.2.3 -Push
    Собрать и запушить в ghcr.io под именем <owner>/mars-gateway.

.EXAMPLE
    .\scripts\windows\release.ps1 -Action Tag -Version v1.2.3 -Push
    Поставить и запушить тег — после этого CI опубликует образы.
#>
[CmdletBinding()]
param(
    [ValidateSet("List", "Images", "Tag")]
    [string]$Action = "List",

    [string[]]$Service,

    [string]$Tag = "local",

    [string]$Registry = "ghcr.io",

    [string]$Owner,

    [switch]$Push,

    [string]$Version,

    [string]$Message = "Релиз MARS"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

function Test-MarsSemanticVersion {
    <#
    .SYNOPSIS
        Семантическая версия ли вида `vX.Y.Z` — только ей двигается `latest`.
    #>
    param([Parameter(Mandatory)][string]$Value)

    return $Value -match '^v[0-9]+(\.[0-9]+)*$'
}

function Get-MarsImageName {
    <#
    .SYNOPSIS
        Имя образа: локальное для compose или полное для registry.
    #>
    param(
        [Parameter(Mandatory)][string]$ServiceName,
        [Parameter(Mandatory)][string]$RepositoryOwner
    )

    if ($Push) {
        return "$Registry/$RepositoryOwner/mars-$ServiceName"
    }

    return "mars-$ServiceName"
}

function Invoke-MarsReleaseTag {
    <#
    .SYNOPSIS
        Проверяет состояние репозитория и ставит аннотированный тег.
    .DESCRIPTION
        Грязное дерево запрещено намеренно: тег указывает на коммит, а не на
        рабочую копию, и незакоммиченные изменения в релизе просто не появятся.
    #>
    Push-Location $script:MarsRoot

    try {
        if ($status = & git status --porcelain) {
            throw (
                "Рабочее дерево не чистое, тег не ставится. Сначала закоммитьте:" +
                [Environment]::NewLine + $status
            )
        }

        $existing = & git tag --list $Version

        if ($existing) {
            throw "Тег '$Version' уже есть. Повторный релиз под тем же тегом перетирает образы."
        }

        $remote = & git ls-remote --tags origin "refs/tags/$Version"

        if ($remote) {
            throw "Тег '$Version' уже есть в origin. Выпускать релиз под занятым тегом нельзя."
        }

        & git tag -a $Version -m $Message

        if ($LASTEXITCODE -ne 0) {
            throw "Тег '$Version' не создан."
        }

        Write-Success "Тег '$Version' создан локально."

        if ($Push) {
            Write-Info "Пуш тега '$Version' в origin запустит release-microservices.yml и опубликует образы."

            $null = Invoke-MarsToolOrFail `
                -FilePath "git" `
                -Arguments @("push", "origin", "refs/tags/$Version") `
                -Description "Пуш тега"
        }
        else {
            Write-Note "Тег не запушен. Публикации не будет, пока тег не уедет в origin."
        }
    }
    finally {
        Pop-Location
    }
}

Push-Location $script:MarsRoot
try {
    if ($Action -eq "List") {
        Write-Step "Матрица публикации (release-microservices.yml)"
        Show-MarsReleaseService

        Write-Note "Сервисы, которых здесь нет, образов не получают: матрица выше и есть правда."
        return
    }

    Assert-MarsCommand -Name "docker" -Hint "Нужен Docker с buildx: сборка идёт multi-stage."

    if ($Action -eq "Tag") {
        if ([string]::IsNullOrWhiteSpace($Version)) {
            throw "Действию Tag нужен -Version, например v1.2.3."
        }

        if (-not (Test-MarsSemanticVersion -Value $Version)) {
            throw (
                "Тег '$Version' не похож на vX.Y.Z. Публикация запускается только тегом v*," +
                " а latest двигается лишь семантической версией."
            )
        }

        Invoke-MarsReleaseTag
        return
    }

    # Images
    $targets = @(Select-MarsReleaseService -Service $Service)
    $shortSha = (& git rev-parse --short HEAD)

    if ($LASTEXITCODE -ne 0) {
        throw "Не удалось получить короткий хеш HEAD: репозиторий git не инициализирован?"
    }

    $repositoryOwner = $Owner

    if ([string]::IsNullOrWhiteSpace($repositoryOwner)) {
        $repositoryOwner = Get-MarsGitRemote

        if ([string]::IsNullOrWhiteSpace($repositoryOwner)) {
            if ($Push) {
                throw "Не удалось определить владельца репозитория из origin. Задайте -Owner явно."
            }

            $repositoryOwner = "local"
        }
    }

    Write-Step "Сборка образов: $($targets.Count), тег $Tag"

    if ($Push) {
        Write-Info "Registry: $Registry/$repositoryOwner"
        Write-Info "Публикация идёт от вашей docker-сессии: docker login $Registry обязателен заранее."
    }

    $failed = @()

    foreach ($item in $targets) {
        $name = Get-MarsImageName -ServiceName $item.Service -RepositoryOwner $repositoryOwner
        $tags = @("$name`:$Tag", "$name`:sha-$shortSha")

        if (Test-MarsSemanticVersion -Value $Tag) {
            $tags += "$name`:latest"
        }

        $arguments = @(
            "build",
            "--file", $item.DockerfilePath,
            "--target", "final"
        )

        foreach ($imageTag in $tags) {
            $arguments += @("--tag", $imageTag)
        }

        if ($Push) {
            $arguments += "--push"
        }

        $arguments += $item.Context

        Write-Info "$($item.Service): $($item.DockerfilePath)"

        $exit = Invoke-MarsTool -FilePath "docker" -Arguments $arguments

        if ($exit -ne 0) {
            Write-Failure "$($item.Service) не собрался (код $exit)"
            $failed += $item.Service
        }
        else {
            Write-Success "$($item.Service) — $($tags -join ', ')"
        }
    }

    Write-Step "Итог"

    if ($failed.Count -gt 0) {
        Write-Failure "Не собраны: $($failed -join ', ')"
        exit 1
    }

    if (-not $Push) {
        Write-Note "Образы собраны локально. Запушить их можно отдельно: docker push <имя>:<тег>."
        Write-Note "Чтобы релиз появился в registry, нужен тег версии: .\scripts\windows\release.ps1 -Action Tag -Version v1.2.3 -Push"
    }
    else {
        Write-Success "Образы запушены в $Registry/$repositoryOwner."
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
