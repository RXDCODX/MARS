<#
.SYNOPSIS
    Поиск хвостов: старое имя, переменная или порт по всему репозиторию.

.DESCRIPTION
    Самая частая ошибка этого репозитория — переименовать или выпилить что-то в
    «своих» файлах и оставить хвосты в остальных. Собственный diff этого не
    показывает: шаблоны `.env.*.example` просто не входят в список изменённых, а
    стенд при этом работает. Так терялись `SEQ_ADMIN_PASSWORD`, `Loki__Url` и
    `LogsDb` при выпиливании Seq, и переменные окружения при переходе
    Jaeger → Tempo.

    Скрипт ищет по файлам, а не по `git grep`: `git grep` смотрит только
    индексированные файлы, а новые в индекс не попали — на этой машине
    `git grep --untracked` возвращал 0 совпадений вместо того, чтобы добавить
    неотслеживаемые.

    Совпадения после прогона бывают трёх видов, и это нормально:

    - объясняющий комментарий («раньше здесь был…») — оставить;
    - «почему так» в `README.md` / `AGENTS.md` — оставить;
    - **рабочая ссылка**: переменная окружения, имя сервиса, порт, путь, имя
      образа — удалить.

.EXAMPLE
    .\scripts\windows\sweep.ps1 -Term SEQ_ADMIN_PASSWORD
    Убрано ли упоминание Seq из репозитория.

.EXAMPLE
    .\scripts\windows\sweep.ps1 -Term 3000, 9155
    Два порта сразу.

.EXAMPLE
    .\scripts\windows\sweep.ps1 -Term Loki__Url -CaseSensitive
    Регистр важен: `loki__url` и `Loki__Url` — разные строки.

.NOTES
    `obj`, `bin`, `node_modules`, `.git`, `.vs`, отчёты покрытия и
    `graphify-out` не обходятся: там копии исходников, и совпадение в них
    ничего не значит. Список — в `common.ps1`.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$Term,

    [string]$Path = ".",

    [switch]$CaseSensitive
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$patterns = @(
    "*.cs", "*.json", "*.yml", "*.yaml", "*.md", "*.props", "*.csproj",
    "*.ps1", "*.sh",
    # `.env*`, а не `*.env`: файлы окружений названы `.env.development` и
    # `.env.production`, и старый шаблон их не видел — мёртвая переменная в
    # боевом окружении всплыла бы только при переименовании.
    ".env*", "*.example", "*.alloy", "*.slnx", "*.ts",
    "*.tsx", "*.js", "*.sql", "Dockerfile", "*.conf", "*.txt", "*.cshtml"
)

Push-Location $script:MarsRoot
try {
    Write-Step "Sweep: $($Term -join ', ')"

    $files = @(Get-MarsSearchFile -Root $Path -Include $patterns)

    Write-Info "Файлов к проверке: $($files.Count)"

    $total = 0

    foreach ($term in $Term) {
        Write-Step "Ищем: $term"

        $found = @(
            $files |
                Select-String -Pattern ([regex]::Escape($term)) -CaseSensitive:$CaseSensitive -ErrorAction SilentlyContinue
        )

        if ($found.Count -eq 0) {
            Write-Success "Совпадений нет."
            continue
        }

        foreach ($hit in $found) {
            $relative = $hit.Path.Replace((Get-Location).Path + "\", "")

            $line = ($hit.Line -replace "\s+", " ").Trim()

            if ($line.Length -gt 140) {
                $line = $line.Substring(0, 140) + "…"
            }

            Write-Output ("{0}:{1}: {2}" -f $relative, $hit.LineNumber, $line)
            $total++
        }

        Write-Note "Совпадений: $($found.Count). Разберите каждое: рабочая ссылка удаляется, объяснение остаётся."
    }

    Write-Step "Итог: совпадений $total"

    if ($total -eq 0) {
        Write-Success "Чисто."
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
