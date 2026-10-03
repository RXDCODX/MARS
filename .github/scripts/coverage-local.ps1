<#
.SYNOPSIS
    Локальный прогон покрытия кода — ровно тот же путь, что и в CI.

.DESCRIPTION
    Задача `coverage` в .github/workflows/ci.yml считает покрытие по слитому отчёту
    и проверяет порог 95% по методам. В CI это видно только в логах GitHub, а
    менять код удобнее локально, поэтому нужен тот же замер здесь — иначе число
    «покрыли ли мы сервис X» приходится угадывать по зелёности тестов.

    Скрипт повторяет шаги задачи `coverage` один в один, меняется только
    источник отчётов: вместо скачивания артефактов `coverage-*` их здесь
    порождают те же самые 17 матричных прогонов, только локально.

    Шаги:
      1. (опционально) `dotnet build MARS.slnx -c Release` — иначе каждый из
         прогонов сначала собирает своё, и время уходит впустую;
      2. для каждого тестового проекта `dotnet test` с `--coverlet` — так же,
         как в матрице `tests`, с теми же четырьмя фильтрами покрытия;
      3. `dotnet reportgenerator` сливает отчёты в `coverage-local/coverage-report`;
      4. `.github/scripts/coverage-gate.py` проверяет порог по слитому отчёту.

    Слияние шага 3 обязательно и локально: MARS.Shared (и почти каждый
    сервис) инструментируется в нескольких тестовых проектах, и простая сумма
    посчитала бы общие методы по разу на каждый проект.

.EXAMPLE
    # Полный честный замер, как в CI
    .\.github\scripts\coverage-local.ps1

.EXAMPLE
    # Только три проекта — быстрый ответ «покрыли ли мы сервис X»
    .\.github\scripts\coverage-local.ps1 -Project MARS.Shared.Tests,MARS.OBS.Tests -NoGate

.EXAMPLE
    # Переиспользовать уже собранные отчёты, пересобрать только слияние
    .\.github\scripts\coverage-local.ps1 -SkipTests

.NOTES
    Пустой набор отчётов считается ошибкой, а не нулём процентов: сломанная
    выгрузка не должна давать зелёный статус при нулевом покрытии. Проверку
    списка тестовых проектов на совпадение с матрицей ci.yml скрипт делает
    сам — иначе новый тестовый проект молча выпадает из отчёта.
#>
[CmdletBinding()]
param(
    # Ограничить прогон конкретными тестовыми проектами. Внимание: слитый
    # отчёт получится неполным, и процент покрытия нельзя сравнивать с CI.
    [string[]]$Project,

    # Не собирать решение перед прогоном (быстрые итерации над тестами).
    [switch]$SkipBuild,

    # Не гонять тесты, а пересобрать только слияние и проверку порога по
    # отчётам, которые уже лежат в каталоге отчётов.
    [switch]$SkipTests,

    # Сколько тестовых проектов гонять одновременно.
    [ValidateRange(1, 32)]
    [int]$MaxParallel = 8,

    # Порог покрытия методами, в процентах. Должен совпадать с
    # COVERAGE_MIN_METHODS в ci.yml.
    [double]$ThresholdMethods = 95,

    # Куда складывать отчёты и слияние. По умолчанию — вне git.
    [string]$OutputDir = "coverage-local",

    # Не проверять порог (например, при прогоне подмножества проектов).
    [switch]$NoGate
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Checked {
    <#
    Запускает внешнюю программу, печатает её вывод (stdout и stderr) и
    возвращает код возврата.

    Обёртка нужна из-за PowerShell 5.1: при $ErrorActionPreference = "Stop"
    любой вывод в stderr от нативной программы превращается в
    terminating NativeCommandError. coverage-gate.py пишет в stderr
    осмысленное сообщение «покрытие ниже порога», и скрипт падал на нём
    раньше, чем успевал показать разбор пробелов, — то есть ровно там, где
    его помощь нужнее всего.
    #>
    param(
        [string]$FilePath,
        [string[]]$ArgumentList,
        [string]$Tool
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & $FilePath @ArgumentList 2>&1 | ForEach-Object { Write-Host $_ }
        return $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

# Пути считаем от корня репозитория, а не от текущего каталога: скрипт
# вызывают и из корня, и из .github/scripts.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Push-Location $repoRoot
try {
    $ciWorkflow = ".\.github\workflows\ci.yml"
    $gateScript = ".\.github\scripts\coverage-gate.py"
    $resultsRoot = Join-Path $OutputDir "TestResults"
    $reportsRoot = Join-Path $OutputDir "reports"
    $reportDir = Join-Path $OutputDir "coverage-report"

    # Те же значения, что в ci.yml. Дублируются намеренно: если их поменять в
    # одном месте и забыть про другое, локальное число перестанет быть
    # сравнимым с CI, а это худший вид расхождения — тихий.
    $coverletInclude = '[MARS.*]*'
    $coverletExclude = '[*.Test*]*'
    $coverletExcludeByFile = '**/Migrations/**'

    # --- Тестовые проекты ------------------------------------------------
    # Источник правды — каталог tests/, а не матрица ci.yml: иначе проект,
    # забытый в матрице, выпал бы и из локального отчёта, и это не было бы
    # видно.
    # @() обязателен: при одном проекте конвейер PowerShell отдаёт скаляр,
    # а у скаляра нет .Count, и Set-StrictMode превращает это в ошибку.
    # Имя оканчивается на .Tests: MARS.TestKit — библиотека общих проверок, а
    # не тестовый проект, и `dotnet test` на ней нечего запускать.
    #
    # $coverageExcluded исключается ДО сверки с матрицей. MARS.ClientUi.Tests
    # — навигационные тесты клиента на Playwright: они открывают живой стенд и
    # не ссылаются ни на один проект из src/, поэтому измерять нечего и
    # coverlet выдал бы пустой отчёт. В матрицу tests он тоже не входит — его
    # гоняет отдельная задача e2e. Если бы он остался в $discovered, скрипт
    # упал бы на сверке с честным сообщением «проект не в матрице», и
    # пришлось бы выбирать между молчаливым пропуском и исключением.
    $coverageExcluded = @("MARS.ClientUi.Tests")

    $discovered = @(Get-ChildItem -Path ".\tests" -Directory |
        Where-Object { $_.Name.EndsWith(".Tests") } |
        Where-Object { $_.Name -notin $coverageExcluded } |
        Where-Object { Test-Path (Join-Path $_.FullName ($_.Name + ".csproj")) } |
        ForEach-Object { $_.Name } |
        Sort-Object)

    if (-not $discovered) {
        throw "В tests/ не найдено ни одного проекта. Проверьте путь запуска."
    }

    # Сверка с матрицей CI. Расхождение в любую сторону — ошибка: и
    # отсутствующий в матрице проект, и проект-призрак в матрице означают,
    # что локальное покрытие и покрытие в CI считаются для разного набора кода.
    $ciText = Get-Content -LiteralPath $ciWorkflow -Raw
    $matrixPattern = '(?m)^\s*-\s*(MARS\.[A-Za-z0-9.]*\.Tests)\s*$'
    $inMatrix = @([regex]::Matches($ciText, $matrixPattern) |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique)

    $missingInMatrix = @($discovered | Where-Object { $inMatrix -notcontains $_ })
    $missingOnDisk = @($inMatrix | Where-Object { $discovered -notcontains $_ })

    if ($missingInMatrix) {
        throw ("Тестовый проект есть в tests/, но его нет в матрице tests в ci.yml: " +
            ($missingInMatrix -join ", ") +
            ". Без записи в матрицу он не проверяется в CI вообще, а его покрытие " +
            "не попадёт в слитый отчёт.")
    }
    if ($missingOnDisk) {
        throw ("Матрица tests в ci.yml ссылается на несуществующий проект: " +
            ($missingOnDisk -join ", ") + ". Задача упадёт на restore.")
    }

    $targets = @(
        if ($Project) { $Project | Sort-Object } else { $discovered }
    )

    $unknown = @($targets | Where-Object { $discovered -notcontains $_ })
    if ($unknown) {
        throw "Нет такого тестового проекта в tests/: $($unknown -join ', ')"
    }

    Write-Host "Тестовых проектов в tests/: $($discovered.Count)"
    if ($Project) {
        Write-Host "Из них в прогоне: $($targets.Count) ($($targets -join ', '))"
    }
    Write-Host "Порог покрытия методами: $ThresholdMethods%"
    Write-Host ""

    # --- Сборка ----------------------------------------------------------
    if (-not $SkipBuild) {
        Write-Host "==> Сборка решения"
        & dotnet build MARS.slnx -c Release
        if ($LASTEXITCODE -ne 0) { throw "Сборка не прошла. Покрытие на красной сборке бессмысленно." }
        Write-Host ""
    }

    # --- Тесты с покрытием ------------------------------------------------
    if (-not $SkipTests) {
        # Старые отчёты снимаем, иначе в слияние попадёт прошлый замер
        # проекта, который в этом прогоне почему-то не отдал свой.
        if (Test-Path $resultsRoot) { Remove-Item -Recurse -Force $resultsRoot }
        New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null

        Write-Host "==> Тесты с покрытием (параллельно $MaxParallel)"

        # Параллелизм через Start-Process, а не через runspace'ы: PowerShell 5.1
        # не умеет Start-ThreadJob, а каждый процесс `dotnet test` всё равно
        # уходит в отдельный процессор. Логи пишем рядом с отчётами — на
        # красном проекте его лог и есть то, что нужно смотреть.
        function Start-CoverageTest {
            param(
                [string]$ProjectName,
                [string]$ResultsRoot,
                [string]$Include,
                [string]$Exclude,
                [string]$ExcludeByFile
            )

            $results = Join-Path $ResultsRoot $ProjectName
            New-Item -ItemType Directory -Force -Path $results | Out-Null
            $log = Join-Path $results "test.log"

            $arguments = @(
                "test", "tests\$ProjectName\$ProjectName.csproj",
                "-c", "Release",
                "--results-directory", $results,
                "--",
                "--coverlet",
                "--coverlet-output-format", "cobertura",
                "--coverlet-file-prefix", $ProjectName,
                "--coverlet-include", $Include,
                "--coverlet-exclude", $Exclude,
                "--coverlet-exclude-by-file", $ExcludeByFile,
                "--report-xunit-trx"
            )

            $process = Start-Process -FilePath "dotnet" -ArgumentList $arguments `
                -NoNewWindow -PassThru `
                -RedirectStandardOutput $log -RedirectStandardError "$log.err"

            # .Handle читается до старта процесса и без этого ExitCode после
            # завершения остаётся пустым: PowerShell 5.1 не кэширует дескриптор
            # у Process-объекта, отданного Start-Process, и .NET не может
            # опросить код возврата. Обойти это можно только так.
            $null = $process.Handle

            return [PSCustomObject]@{
                Name    = $ProjectName
                Process = $process
                Log     = $log
            }
        }

        $queue = [System.Collections.Generic.List[string]]::new()
        foreach ($name in $targets) { $queue.Add($name) }

        $running = @()
        $failures = @()

        while ($queue.Count -gt 0 -or $running.Count -gt 0) {
            while ($queue.Count -gt 0 -and $running.Count -lt $MaxParallel) {
                $next = $queue[0]
                $queue.RemoveAt(0)
                $running += Start-CoverageTest `
                    -ProjectName $next `
                    -ResultsRoot $resultsRoot `
                    -Include $coverletInclude `
                    -Exclude $coverletExclude `
                    -ExcludeByFile $coverletExcludeByFile
                Write-Host "    $next — запущены"
            }

            Start-Sleep -Milliseconds 400

            $stillRunning = @()
            foreach ($item in $running) {
                if ($item.Process.HasExited) {
                    if ($item.Process.ExitCode -eq 0) {
                        Write-Host "    $($item.Name) — зелёные"
                    }
                    else {
                        Write-Host "    $($item.Name) — КРАСНЫЕ, лог: $($item.Log)"
                        $failures += $item.Name
                    }
                }
                else {
                    $stillRunning += $item
                }
            }
            $running = $stillRunning
        }

        if ($failures.Count -gt 0) {
            $list = ($failures | Sort-Object) -join ", "
            throw "Упали тестовые проекты: $list. Покрытие на упавших тестах мерять рано — сперва чиним тесты."
        }
        Write-Host ""
    }

    # --- Сбор отчётов по проектам ----------------------------------------
    # Отчёты лежат в подпапке на проект: так их не перетирает следующий прогон.
    if (Test-Path $reportsRoot) { Remove-Item -Recurse -Force $reportsRoot }
    New-Item -ItemType Directory -Force -Path $reportsRoot | Out-Null

    $found = @(Get-ChildItem -Path $resultsRoot -Recurse -File -Filter "*.cobertura*.xml" -ErrorAction SilentlyContinue)
    if (-not $found) {
        throw ("Не найдено ни одного отчёта cobertura в $resultsRoot. " +
            "Нулевой набор отчётов — это ошибка выгрузки, а не нулевое покрытие: " +
            "проверьте, что в тестовых проектах есть PackageReference coverlet.MTP.")
    }
    foreach ($file in $found) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $reportsRoot $file.Name) -Force
    }
    Write-Host "Отчётов cobertura: $($found.Count)"
    Write-Host ""

    # --- Слияние ---------------------------------------------------------
    if (Test-Path $reportDir) { Remove-Item -Recurse -Force $reportDir }

    # ReportGenerator лежит в локальном манифесте .config/dotnet-tools.json, а не
    # в системном наборе: на свежей клоне `dotnet reportgenerator` не найдётся,
    # и замер падал бы на «инструмент не установлен» вместо покрытия. В CI ту же
    # строку делает шаг «Восстановить локальные инструменты».
    $toolRestoreExit = Invoke-Checked -FilePath "dotnet" -ArgumentList @(
        "tool", "restore"
    ) -Tool "dotnet tool restore"
    if ($toolRestoreExit -ne 0) { throw "Локальные инструменты не восстановились: нужен reportgenerator из .config/dotnet-tools.json." }

    $generatorExit = Invoke-Checked -FilePath "dotnet" -ArgumentList @(
        "reportgenerator",
        "-reports:$reportsRoot/**/*.cobertura*.xml",
        "-targetdir:$reportDir",
        "-reporttypes:HtmlInline_AzurePipelines_Dark;Cobertura;TextSummary"
    ) -Tool "ReportGenerator"
    if ($generatorExit -ne 0) { throw "ReportGenerator не отработал." }
    Write-Host ""

    # --- Порог -----------------------------------------------------------
    $gateArgs = @(
        $gateScript,
        "--merged", (Join-Path $reportDir "Cobertura.xml"),
        "--reports-dir", $reportsRoot,
        "--reports-pattern", "*.cobertura*.xml",
        "--threshold-methods", $ThresholdMethods
    )

    if ($Project) {
        Write-Warning ("Прогон по подмножеству ($($targets.Count) из $($discovered.Count)): " +
            "покрытие в слитом отчёте ниже, чем в CI, и проценты несравнимы.")
    }

    if ($NoGate) {
        Write-Host "==> Проверка порога пропущена (-NoGate)"
        $null = Invoke-Checked -FilePath "python" -ArgumentList $gateArgs -Tool "coverage-gate.py"
    }
    else {
        Write-Host "==> Проверка порога"
        $gateExit = Invoke-Checked -FilePath "python" -ArgumentList $gateArgs -Tool "coverage-gate.py"
        if ($gateExit -ne 0) {
            Write-Host ""
            Write-Host "Порог не пройден. Список самых непокрытых классов:"
            Write-Host ""
            $null = Invoke-Checked -FilePath "python" -ArgumentList @(
                ".\.github\scripts\coverage-gaps.py",
                "--merged", (Join-Path $reportDir "Cobertura.xml"),
                "--top", "40"
            ) -Tool "coverage-gaps.py"
            exit 1
        }
    }
}
finally {
    Pop-Location
}
