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

.PARAMETER Check
    Только проверить. Код возврата 1, если профиль разошёлся с данными.

.NOTES
    Профиль осознанно не задаёт URL сервиса и не полагается на applicationUrl у
    gRPC-сервисов: `AddMarsGrpcHosting` слушает через `ListenAnyIP`, который
    подавляет и ASPNETCORE_URLS, и applicationUrl. Там порт приходит из
    Grpc__HttpPort/Grpc__GrpcPort того же профиля.
#>
[CmdletBinding()]
param(
    [switch]$Check
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
    exit $LASTEXITCODE
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}
finally {
    Pop-Location
}
