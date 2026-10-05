<#
.SYNOPSIS
    Локальный замер покрытия — вход в `.github/scripts/coverage-local.ps1`.

.DESCRIPTION
    Скрипт намеренно ничего не измеряет сам, а передаёт параметры каноническому
    `coverage-local.ps1`. Причина в том, что покрытие here — число, с которым
    сравнивают CI, а CI считает его по конкретным шагам: сборка решения, 17
    прогонов с `--coverlet` и четырьмя фильтрами, слияние ReportGenerator,
    гейт по методам. Вторая реализация того же пути разошлась бы с первой при
    первом же изменении фильтра — и разошлась бы молча, потому что обе
    печатали бы правдоподобный процент.

    Параметры здесь те же, что у `coverage-local.ps1`, и передаются напрямую.

    Порог в методах (`-ThresholdMethods 95`) обязан совпадать с
    `COVERAGE_MIN_METHODS` в `ci.yml`: расхождение означало бы, что локально
    зелёный замер в CI красный.

    Про `-Project`: слитый отчёт по подмножеству проектов неполон, и его процент
    нельзя сравнивать с CI. Для ответа «покрыли ли мы сервис X» это годится.

.EXAMPLE
    .\scripts\windows\coverage.ps1
    Полный честный замер, как в CI.

.EXAMPLE
    .\scripts\windows\coverage.ps1 -Project MARS.OBS.Tests -NoGate
    Быстрый ответ по одному сервису.

.EXAMPLE
    .\scripts\windows\coverage.ps1 -SkipTests
    Пересобрать только слияние и порог по уже прогнанным тестам.

.NOTES
    На unix тот же замер делает `scripts/unix/coverage.sh`: там канонического
    PowerShell-скрипта нет, и поведение повторяет его шаги.
#>
[CmdletBinding()]
param(
    [string[]]$Project,

    [switch]$SkipBuild,

    [switch]$SkipTests,

    [ValidateRange(1, 32)]
    [int]$MaxParallel = 8,

    [ValidateRange(0, 100)]
    [double]$ThresholdMethods = 95,

    [string]$OutputDir = "coverage-local",

    [switch]$NoGate
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$canonical = Join-Path $script:MarsRoot ".github\scripts\coverage-local.ps1"

if (-not (Test-Path -LiteralPath $canonical)) {
    throw "Не найден канонический скрипт замера покрытия: $canonical"
}

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "dotnet" -Hint "Нужен .NET SDK версии из global.json (10.0.x)."
    $null = Get-MarsPythonCommand

    Write-Step "Покрытие: делегирую $canonical"

    # Параметры собираются списком, а не позиционными: у coverage-local.ps1 их
    # девять, и порядок positional-параметров пришлось бы дублировать здесь
    # вторым списком — с понятной перспективой разъехаться.
    $arguments = @{
        MaxParallel       = $MaxParallel
        ThresholdMethods  = $ThresholdMethods
        OutputDir         = $OutputDir
        SkipBuild         = [bool]$SkipBuild
        SkipTests         = [bool]$SkipTests
        NoGate            = [bool]$NoGate
    }

    if ($Project -and $Project.Count -gt 0) {
        $arguments["Project"] = $Project
    }

    Write-Info "Порог по методам: $ThresholdMethods%"
    Write-Info "Отчёты: $OutputDir (каталог в .gitignore)"

    & $canonical @arguments

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    Write-Success "Гейт покрытия пройден."
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
