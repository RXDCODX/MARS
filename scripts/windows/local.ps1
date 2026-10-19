<#
.SYNOPSIS
    Пересобирает профиль запуска MARS.Local в src/*/Properties/launchSettings.json.

.DESCRIPTION
    Профиль описывает сервис, запущенный НА ХОСТЕ из IDE (F5), при инфраструктуре
    в Docker. Без него локальный запуск не работает: в appsettings.json описан
    стенд контейнеров (RabbitMq:Host там rabbitmq), а контейнерные имена с хоста
    не резолвятся, и сервис падает на подключении к брокеру.

    Обёртка только находит корень репозитория и зовёт scripts/local_launch.py:
    реализация генератора одна на обе платформы намеренно, как гейт покрытия.
    Своя копия алгоритма на PowerShell разошлась бы с unix-версией молча — и
    разошлась бы именно в формате, который проверяет -Check.

    Данные профиля — scripts/local-launch.conf: общий блок плюс блок сервиса.
    Подстановки ${ИМЯ} разрешает ШАБЛОН окружения (.env.development.example), а не
    файл с секретами: иначе настоящий токен уехал бы в коммит. Ключи из строк
    `#secrets` в профиль не попадают вовсе — их значения кладут в user-secrets,
    и скрипт заканчивается напоминанием об этом.

.EXAMPLE
    .\scripts\windows\local.ps1
    Пересобирает профиль MARS.Local у всех сервисов.

.EXAMPLE
    .\scripts\windows\local.ps1 -Check
    Ничего не пишет и падает, если профиль разошёлся с local-launch.conf.

.EXAMPLE
    .\scripts\windows\local.ps1 -Run MARS.TwitchCore
    Пересобирает профиль, поднимает базу и брокер, затем запускает сервис.

.PARAMETER Check
    Только проверить. Код возврата 1, если профиль разошёлся с данными.

.PARAMETER Run
    Каталог сервиса в src\ (например, MARS.TwitchCore). После пересборки профиля
    поднимает из compose базу и брокер, фоном — наблюдаемость, и запускает сервис
    по профилю MARS.Local. Аргументы после `--` уходят самому сервису.

.NOTES
    Профиль осознанно не задаёт URL сервиса и не полагается на applicationUrl у
    gRPC-сервисов: `AddMarsGrpcHosting` слушает через `ListenAnyIP`, который
    подавляет и ASPNETCORE_URLS, и applicationUrl. Там порт приходит из
    Grpc__HttpPort/Grpc__GrpcPort того же профиля.
#>
[CmdletBinding()]
param(
    [switch]$Check,

    [string]$Run,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RunArgument
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

Push-Location $script:MarsRoot
try {
    $python = Get-MarsPythonCommand
    $arguments = @("scripts/local_launch.py", "--root", ".")

    if ($Check) {
        $arguments += "--check"
    }

    & $python @arguments

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    if ($Run) {
        $project = Join-Path $script:MarsRoot (Join-Path "src" $Run)

        if (-not (Test-Path -LiteralPath (Join-Path $project "$Run.csproj"))) {
            throw (
                "Нет проекта src\$Run. Имя сервиса — это каталог в src\" +
                " (например, MARS.TwitchCore)."
            )
        }

        Write-Step "Инфраструктура: $($script:MarsInfraServices -join ', ')"

        # Паритет ключей нужен и здесь: поднимаем ту же среду, что и `stack`, а
        # отставший файл окружения даёт случайный хост-порт — то есть сервис,
        # который молча пошёл не к тому стенду.
        $template = Join-Path $script:MarsRoot ".env.development.example"

        if (Test-Path -LiteralPath $template) {
            $environmentFile = Initialize-MarsEnvironmentFile -Dev
            $null = Test-MarsEnvironmentFileParity -File $environmentFile -Template $template
        }

        Assert-MarsCommand -Name "docker" -Hint "Нужен Docker: инфраструктура живёт в контейнерах."

        $compose = @("compose") + @(Get-MarsComposeArgument -Dev)

        # Без базы и брокера сервис не проходит: миграции ждут базу ~50 с и
        # падают, а RabbitMQ пишет бесконечный реконнект. Поэтому фаза блокирующая:
        # ждём именно готовности, а не «контейнер запущен».
        $null = Invoke-MarsToolOrFail -FilePath "docker" `
            -Arguments ($compose + @("up", "-d", "--wait", "--wait-timeout") +
                @($script:MarsInfraWaitTimeout) + @($script:MarsInfraServices)) `
            -Description "compose up $($script:MarsInfraServices -join ' ')"

        # Наблюдаемость — фоном и без --wait: недоступный образ Grafana не должен
        # срывать запуск сервиса. Её список — $script:MarsInfraBackgroundServices.
        $null = Invoke-MarsTool -FilePath "docker" `
            -Arguments ($compose + @("up", "-d") + @($script:MarsInfraBackgroundServices))

        Write-Step "Запуск $Run по профилю MARS.Local"

        $dotnetArguments = @(
            "run", "--project", (Join-Path "src" (Join-Path $Run "$Run.csproj")),
            "--launch-profile", "MARS.Local"
        )

        if ($null -ne $RunArgument -and $RunArgument.Count -gt 0) {
            # `--` отделяет аргументы сервиса от аргументов `dotnet run`, поэтому
            # разделитель скрипта (`local -Run X -- --urls …`) не переносится.
            $forwarded = @($RunArgument | Where-Object { $_ -ne "--" })

            if ($forwarded.Count -gt 0) {
                $dotnetArguments += @("--") + $forwarded
            }
        }

        $null = Invoke-MarsTool -FilePath "dotnet" -Arguments $dotnetArguments
        exit $LASTEXITCODE
    }
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}
finally {
    Pop-Location
}
