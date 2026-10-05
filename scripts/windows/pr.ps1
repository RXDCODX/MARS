<#
.SYNOPSIS
    Ветка, пуш и Pull Request — по порядку, с гейтом между шагами.

.DESCRIPTION
    Работа в этом репозитории идёт так: отдельная ветка, зелёные сборка и
    тесты локально, пуш своей ветки, PR с заполненным описанием. Скрипт
    выполняет последовательность и **не делает две вещи**: не коммитит (коммит —
    это логический шаг, решает человек) и не мержит (merge — решение владельца).

    Что скрипт добавляет к описанию в `AGENTS.md`:

    - отказывается работать на `main` — пуш в `main` запрещён;
    - прогоняет `verify.ps1` перед пушем, если не сказано иное;
    - запускает `gh workflow run ci.yml --ref <ветка>`. Push, сделанный
      `GITHUB_TOKEN`, не запускает workflow (защита GitHub от рекурсии), поэтому
      обязательные статусы в branch protection иначе не закрылись бы никогда;
    - требует текст описания: пустой PR без описания — не считается сделанной
      работой, а формальные разделы хуже отсутствующих.

.PARAMETER Title
    Заголовок PR в стиле conventional-коммитов, по-русски: `feat: …`.

.PARAMETER Body
    Текст описания. Разделы — «Что / Зачем / Как / Проверка / Миграция и откат /
    Сквозные правки». Без `-Body` и без `-BodyFile` скрипт печатает шаблон и
    ничего не делает.

.PARAMETER Base
    Ветка, в которую открывается PR. По умолчанию `main`.

.EXAMPLE
    .\scripts\windows\pr.ps1 -PrintTemplate
    Шаблон описания — заполнить в файл.

.EXAMPLE
    .\scripts\windows\pr.ps1 -Title "feat: RANDOM ART через matoi" -BodyFile .github\pr-body.md
    Прогнать гейт, запушить ветку, перезапустить CI и открыть PR.
#>
[CmdletBinding()]
param(
    [string]$Title,

    [string]$Body,

    [string]$BodyFile,

    [string]$Base = "main",

    [switch]$SkipChecks,

    [switch]$SkipWorkflowDispatch,

    [switch]$PrintTemplate
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$template = @"
## Что
<одно предложение: какое поведение изменилось>

## Зачем
<проблема или требование, из которого это следует; ссылка на дефект или задачу>

## Как
<ключевые решения и почему именно так>

## Проверка
<имя тест-проекта и класс/метод теста либо ручной сценарий с командой>

## Миграция и откат
<что нужно владельцу: новые переменные в .env.production (или .env.development), пересоздание тома, перезапуск стенда; как откатить>

## Сквозные правки
<.env.production.example и .env.development.example, compose, Directory.Packages.props, ci.yml, ServiceEndpoints, README, миграции>
"@

function Resolve-MarsPrBody {
    <#
    .SYNOPSIS
        Текст описания из параметра или файла. Без обоих — $null.
    #>
    if (-not [string]::IsNullOrWhiteSpace($Body)) {
        return $Body
    }

    if (-not [string]::IsNullOrWhiteSpace($BodyFile)) {
        if (-not (Test-Path -LiteralPath $BodyFile)) {
            throw "Файл с описанием не найден: $BodyFile"
        }

        return (Get-Content -LiteralPath $BodyFile -Raw)
    }

    return $null
}

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "git" -Hint "Нужен git: скрипт работает с веткой и пушем."
    Assert-MarsCommand -Name "gh" -Hint "Нужен GitHub CLI (gh): PR создаётся через gh pr create."

    $branch = (& git rev-parse --abbrev-ref HEAD)

    if ($LASTEXITCODE -ne 0) {
        throw "Не git-репозиторий или нет HEAD: $script:MarsRoot"
    }

    if ($PrintTemplate) {
        Write-Output $template
        return
    }

    if ($branch -in @("main", "master")) {
        throw (
            "Мы на '$branch'. Работа идёт в отдельной ветке: создайте её до первой правки" +
            " (git switch -c feat/короткое-имя) и повторите."
        )
    }

    if ([string]::IsNullOrWhiteSpace($Title)) {
        throw "Нужен -Title: conventional-коммит по-русски, например 'feat: RANDOM ART через matoi'."
    }

    $body = Resolve-MarsPrBody

    if ([string]::IsNullOrWhiteSpace($body)) {
        Write-Step "Шаблон описания PR"
        Write-Output $template
        throw "Описание обязательно: заполните -Body или -BodyFile. Пустой PR не считается сделанной работой."
    }

    Write-Step "Ветка $branch -> $Base"

    # PR мог быть открыт раньше; gh pr create тогда падает, а состояние ветки
    # всё равно полезно проверить.
    $existing = (& gh pr view --json url -q .url 2>$null)

    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existing)) {
        Write-Note "PR уже открыт: $existing"
    }

    if (-not $SkipChecks) {
        Write-Info "Гейт перед пушем: .\scripts\windows\verify.ps1"
        & (Join-Path $PSScriptRoot "verify.ps1")

        if ($LASTEXITCODE -ne 0) {
            throw "Гейт не пройден — пуш не делаем. Поправить и повторить."
        }
    }

    Write-Step "Пуш ветки"

    $null = Invoke-MarsToolOrFail `
        -FilePath "git" `
        -Arguments @("push", "-u", "origin", $branch) `
        -Description "Пуш ветки $branch"

    # Push, сделанный GITHUB_TOKEN, workflow не запускает: это защита GitHub от
    # рекурсии. На своём рабочем столе push запустит CI сам, но перезапуск через
    # gh нужен после auto-format'а и не мешает в остальных случаях.
    if (-not $SkipWorkflowDispatch) {
        Write-Info "Перезапуск CI на ветке: gh workflow run ci.yml --ref $branch"
        $null = Invoke-MarsTool -FilePath "gh" -Arguments @(
            "workflow", "run", "ci.yml", "--ref", $branch
        )
    }

    if ($existing -and $LASTEXITCODE -eq 0) {
        Write-Success "PR уже был открыт: $existing"
        return
    }

    Write-Step "Создание PR"

    $null = Invoke-MarsToolOrFail `
        -FilePath "gh" `
        -Arguments @("pr", "create", "--base", $Base, "--head", $branch, "--title", $Title, "--body", $body) `
        -Description "Создание PR"

    $url = (& gh pr view --json url -q .url)

    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($url)) {
        Write-Success "PR: $url"
        Write-Note "Merge — решение владельца: gh pr merge скриптом не вызывается."
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
