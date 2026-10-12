<#
.SYNOPSIS
    Пересобирает производные хостовые порты стенда из `MARS_PORT_BASE`.

.DESCRIPTION
    Хостовые порты задаёт одна переменная на среду — `MARS_PORT_BASE`, а сами
    значения лежат в явных `MARS_PORT_<СЕРВИС>`. Арифметики compose не умеет:
    ни `${MARS_PORT_BASE + 17}`, ни `base + номер` он не считает, поэтому сумму
    приходится записывать числом. Скрипт делает это за человека: берёт
    `MARS_PORT_BASE` из файла и переписывает строки `MARS_PORT_<СЕРВИС>` в порядке
    их следования — `base`, `base + 1`, `base + 2` и так далее.

    Порядок строк в блоке и есть нумерация, и это не деталь оформления: сдвинуть
    стенд на другую базу значит поменять одну строку `MARS_PORT_BASE` и запустить
    этот скрипт. Номер каждой переменной охраняет тест
    `PortAllocationTests.КаждыйПортРавенБазовомуПлюсНомер`, а скрипт только
    заполняет файл по нему: расходиться с тестом он не может, потому что схему не
    дублирует, а читает порядок строк из того же файла.

    По умолчанию обрабатываются оба шаблона сред и `.env.example`. Реальный файл
    окружения (`.env.development`, `.env.production`) в список не входит: он в git
    не лежит, и правка чужого файла с секретами не должна быть побочным эффектом
    запуска без аргументов — его передают в `-Path` явно, когда блок портов в нём
    уже есть.

.EXAMPLE
    .\scripts\windows\ports.ps1
    Пересобирает порты всех трёх шаблонов из их `MARS_PORT_BASE`.

.EXAMPLE
    .\scripts\windows\ports.ps1 -Path .env.development -Base 6101
    Переводит стенд разработки на базу 6101 и заполняет порты: 6101, 6102, 6103…

.EXAMPLE
    .\scripts\windows\ports.ps1 -Check
    Ничего не пишет, но падает, если хоть одна строка разошлась с `MARS_PORT_BASE`.

.PARAMETER Path
    Файлы для обработки. По умолчанию: `.env.production.example`,
    `.env.development.example`, `.env.example`.

.PARAMETER Base
    Новое значение `MARS_PORT_BASE`. Без него база берётся из самого файла.

.PARAMETER Check
    Только проверить. Код возврата 1, если значения разошлись с базой.

.NOTES
    Файл без строки `MARS_PORT_BASE` не дополняется: схема должна быть в нём
    целиком, и склеивать её из ничего значило бы расставить номера наугад.
    Такой файл берут из шаблона или переносят в него блок портов.
#>
[CmdletBinding()]
param(
    [string[]]$Path = @(
        ".env.production.example",
        ".env.development.example",
        ".env.example"
    ),

    [int]$Base = 0,

    [switch]$Check
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

function Update-MarsPortFile {
    <#
    .SYNOPSIS
        Переписывает строки `MARS_PORT_<СЕРВИС>` файла по схеме `base + номер`.
    .OUTPUTS
        Число переписанных строк: 0 — файл уже соответствовал схеме.
    #>
    param(
        [Parameter(Mandatory)][string]$File,
        [int]$Base = 0,
        [switch]$Check
    )

    $target = if ([System.IO.Path]::IsPathRooted($File)) {
        $File
    }
    else {
        Join-Path $script:MarsRoot $File
    }

    if (-not (Test-Path -LiteralPath $target)) {
        throw "Нет файла ${File}."
    }

    $text = [System.IO.File]::ReadAllText($target)
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = [regex]::Split($text, "\r?\n")

    $portBase = $Base

    if ($portBase -le 0) {
        $found = $null

        foreach ($line in $lines) {
            if ($line -match '^MARS_PORT_BASE=(?<value>\d+)\s*$') {
                $found = $Matches.value
                break
            }
        }

        if ($null -eq $found) {
            throw "${File} не содержит MARS_PORT_BASE. Задайте базу через -Base или возьмите блок портов из шаблона."
        }

        $portBase = [int]$found
    }

    $index = 0
    $differences = 0
    $result = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        $expected = $line

        if ($line -match '^MARS_PORT_BASE=') {
            $expected = "MARS_PORT_BASE=$portBase"
        }
        elseif ($line -match '^MARS_PORT_(?<key>[A-Z0-9_]+)=(?<value>\d*)\s*$') {
            $expected = "MARS_PORT_$($Matches.key)=$($portBase + $index)"
            $index++
        }

        if ($expected -ne $line) {
            $differences++
            Write-Info "${File}: $line -> $expected"
            $line = $expected
        }

        $result.Add($line)
    }

    if ($differences -eq 0) {
        Write-Success "${File} уже соответствует MARS_PORT_BASE=$portBase."
        return 0
    }

    if ($Check) {
        Write-Note "${File}: расхождений с MARS_PORT_BASE=$portBase — $differences."
        return $differences
    }

    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($target, ($result -join $newline), $utf8)

    Write-Success "${File}: пересобрано строк — $differences (MARS_PORT_BASE=$portBase)."
    return 0
}

Push-Location $script:MarsRoot
try {
    Write-Step "Порты: база $(if ($Base -gt 0) { $Base } else { 'из файла' })"

    $changed = 0

    foreach ($file in $Path) {
        $changed += Update-MarsPortFile -File $file -Base $Base -Check:$Check
    }

    if ($Check) {
        if ($changed -gt 0) {
            throw "Порты разошлись с MARS_PORT_BASE: строк $changed. Запустите скрипт без -Check, чтобы пересобрать."
        }

        Write-Success "Все проверенные файлы соответствуют схеме."
    }
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}
finally {
    Pop-Location
}
