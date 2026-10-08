<#
.SYNOPSIS
    Навигационные тесты клиента: стенд целиком, от файла окружения до логов.

.DESCRIPTION
    Повторяет задачу `e2e` из `ci.yml` один в один. Каждый шаг здесь
    обязателен по причинам, которые уже стоили прогона:

    - `.env.production` создаётся из `.env.production.example`: файлы окружений
      в git не попадают, compose получает нужный флагом `--env-file`, а без
      файла стенд поднимается со значениями по умолчанию, то есть с пустыми
      паролями, и db-init останавливается на первой проверке;
    - `--wait` обязателен: compose возвращается, когда контейнеры *запустились*,
      а не когда они готовы, и тесты пошли бы в Gateway без клиента;
    - `--network host` нужен, чтобы контейнер увидел Gateway на localhost
      раннера: иначе localhost внутри контейнера — это сам контейнер;
    - `--shm-size=1g` обязателен: 64 МБ `/dev/shm` не хватает Chromium, и он
      падает с «Target crashed» без внятной причины;
    - стенд гасится даже при упавших тестах: иначе `docker compose down` не
      выполнится, а логи будут не те.

    Образ тестов собирается здесь, а не в задаче сборки: тесты ходят в Gateway
    на localhost, а Playwright живёт внутри контейнера.

    На Docker Desktop для Windows `--network host` требует включённой опции
    «Host networking» в настройках Docker (Settings → Resources → Network).
    Без неё контейнер не увидит стенд на localhost, и фикстура честно скажет
    об этом минуту спустя.

.PARAMETER Keep
    Не гасить стенд после прогона. Полезно, чтобы посмотреть страницу руками.

.PARAMETER SkipUp
    Стенд уже поднят — не пересобирать и не ждать готовности.

.PARAMETER BaseUrl
    Адрес стенда для Playwright. По умолчанию `http://localhost:10155`, тот же,
    что в compose и в CI.

.EXAMPLE
    .\scripts\windows\e2e.ps1
    Полный цикл: файл окружения, стенд, образ, тесты, гашение стенда.

.EXAMPLE
    .\scripts\windows\e2e.ps1 -Keep
    Оставить стенд поднятым после тестов.

.EXAMPLE
    .\scripts\windows\e2e.ps1 -SkipUp
    Только тесты на уже поднятом стенде.
#>
[CmdletBinding()]
param(
    [switch]$Keep,

    [switch]$SkipUp,

    [string]$BaseUrl = "http://localhost:10155",

    [ValidateRange(30, 1800)]
    [int]$WaitTimeout = 420
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$testImage = "mars-clientui-tests"
$testDockerfile = "tests/MARS.ClientUi.Tests/Dockerfile"

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "docker" -Hint "Нужен Docker: тесты ходят в Gateway, поднятый контейнерами."
    Assert-MarsCommand -Name "dotnet" -Hint "Нужен .NET SDK — образ тестов собирается на SDK."

    $compose = @("compose") + @(Get-MarsComposeArgument)

    Write-Step "Навигационные тесты клиента"

    if (-not $SkipUp) {
        $null = Initialize-MarsEnvironmentFile

        Write-Info "Подъём стенда: docker compose up -d --build --wait"

        $upArguments = $compose + @("up", "-d", "--build", "--wait", "--wait-timeout", $WaitTimeout)
        $upExit = Invoke-MarsTool -FilePath "docker" -Arguments $upArguments

        if ($upExit -ne 0) {
            Write-Failure "Стенд не поднялся (код $upExit). Логи:"
            $null = Invoke-MarsTool -FilePath "docker" -Arguments ($compose + @("logs", "--no-color", "--tail", "200"))
            exit 1
        }
    }
    else {
        Write-Info "Стенд считается поднятым (-SkipUp)"
    }

    Write-Step "Образ тестов"

    # Версия образа обязана совпадать с Microsoft.Playwright в
    # Directory.Packages.props: Playwright сверяет свою версию с версией
    # браузера и падает при расхождении.
    $null = Invoke-MarsToolOrFail `
        -FilePath "docker" `
        -Arguments @("build", "-f", $testDockerfile, "-t", $testImage, ".") `
        -Description "Сборка образа тестов"

    Write-Step "Прогон Playwright"

    $runExit = Invoke-MarsTool -FilePath "docker" -Arguments @(
        "run", "--rm",
        "--network", "host",
        "--shm-size=1g",
        "-e", "MARS_CLIENTUI_BASE_URL=$BaseUrl",
        $testImage
    )

    Write-Step "Итог"

    if (-not $Keep -and -not $SkipUp) {
        Write-Info "Гашение стенда: docker compose down -v"
        $null = Invoke-MarsTool -FilePath "docker" -Arguments ($compose + @("down", "-v"))
    }
    elseif ($Keep) {
        Write-Info "Стенд оставлен поднятым: снять его — .\scripts\windows\stack.ps1 -Action Down -Volumes"
    }

    if ($runExit -ne 0) {
        Write-Failure "Навигационные тесты красные (код $runExit)."

        if (-not $Keep -and -not $SkipUp) {
            Write-Note "Логи стенда уже сняты вместе с ним. Повторить с -Keep, чтобы посмотреть."
        }

        exit $runExit
    }

    Write-Success "Навигационные тесты зелёные."
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
