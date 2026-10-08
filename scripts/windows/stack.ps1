<#
.SYNOPSIS
    Стенд на docker compose: поднять, погасить, посмотреть логи.

.DESCRIPTION
    Обёртка над compose с осмысленными действиями вместо длинных строк.
    Тонкости, которые она держит:

    - `docker-compose.dev.yml` подключается только с `-Dev` и вторым файлом.
      Он переименован из `override` намеренно, чтобы обычный
      `docker compose up` не подхватывал hot-reload (`dotnet watch`) молча;
    - `up` всегда с `--wait`: compose возвращается, когда контейнеры запустились,
      а не когда готовы. Без него следующий шаг (или разработчик) полезет в
      Gateway, который ещё не слушает;
    - `.env.development` (или `.env.production` без `-Dev`) создаётся из своего
      шаблона, если его нет: без него compose берёт значения по умолчанию из
      `${ПЕРЕМЕННАЯ:-}`, стенд поднимается с пустыми паролями, а
      `01-databases.sh` останавливается на первой проверке. Существующий файл не
      перезаписывается — в нём настоящие секреты стенда;
    - `down -v` сносит тома, а без `-Volumes` тома переживают стенд. Осторожно:
      на этом томе живёт `mars-wwwroot`, общий для `obs`, `alerts` и
      `media-storage`, то есть конвейер «алерт → файл».

    Наружу опубликован только Gateway (`10155:8080`), поэтому «зайти на стенд»
    означает `http://localhost:10155`. Grafana при этом на 30000, не на 3000:
    порт 3000 занят контейнером из чужого проекта.

.PARAMETER Action
    Up, Down, Restart, Logs, Ps, Pull, Config.

.PARAMETER Dev
    Подключить `docker-compose.dev.yml`: hot-reload через `dotnet watch`.

.EXAMPLE
    .\scripts\windows\stack.ps1 -Action Up
    Поднять боевой стенд и дождаться готовности.

.EXAMPLE
    .\scripts\windows\stack.ps1 -Action Up -Dev
    Поднять стенд с hot-reload.

.EXAMPLE
    .\scripts\windows\stack.ps1 -Action Logs -Service gateway -Tail 200
    Логи одного сервиса.

.EXAMPLE
    .\scripts\windows\stack.ps1 -Action Down -Volumes
    Погасить стенд вместе с томами — следующий запуск будет с чистой базой.
#>
[CmdletBinding()]
param(
    [ValidateSet("Up", "Down", "Restart", "Logs", "Ps", "Pull", "Config")]
    [string]$Action = "Up",

    [string[]]$Service,

    [switch]$Dev,

    [switch]$Build,

    [switch]$Volumes,

    [ValidateRange(30, 1800)]
    [int]$WaitTimeout = 420,

    [int]$Tail = 200
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

Push-Location $script:MarsRoot
try {
    Assert-MarsCommand -Name "docker" -Hint "Нужен Docker: стенд живёт в контейнерах."

    $compose = @("compose") + @(Get-MarsComposeArgument -Dev:$Dev)
    $label = if ($Dev) { "dev (hot-reload)" } else { "production" }
    $environment = if ($Dev) { ".env.development" } else { ".env.production" }

    Write-Step "compose: $Action, стенд $label, окружение $environment"

# Файл среды нужен любой команде compose, а не только `Up`: с `--env-file`
# compose падает с «couldn't find env file» на отсутствующем файле, то есть
# `logs` и `ps` перестают работать, когда среду не поднимали этой машине.
$null = Initialize-MarsEnvironmentFile -Dev:$Dev

    $arguments = switch ($Action) {
        "Up" {
            $up = @("up", "-d")

            if ($Build) {
                $up += "--build"
            }

            # --wait без --build тоже нужен: сервисы поднимаются по
            # service_healthy, и на холодном томе postgres это минуты.
            $up += @("--wait", "--wait-timeout", $WaitTimeout)
            $up
        }
        "Down" {
            $down = @("down")

            if ($Volumes) {
                Write-Note "Тома будут снесены: базы пересоздаются, mars-wwwroot очищается."
                $down += "-v"
            }

            $down
        }
        "Restart" { @("restart") }
        "Logs" { @("logs", "--no-color", "--follow", "--tail", $Tail) }
        "Ps" { @("ps") }
        "Pull" { @("pull") }
        "Config" { @("config") }
    }

    # Имена сервисов идут после команды compose, а не перед ней:
    # `docker compose -f … gateway up` — это «сервис gateway и подкоманда up»,
    # а compose ждёт подкоманду сразу после -f.
    $serviceArguments = @()

    if ($Service -and $Service.Count -gt 0) {
        $serviceArguments = @($Service)
    }

    $null = Invoke-MarsToolOrFail `
        -FilePath "docker" `
        -Arguments ($compose + $arguments + $serviceArguments) `
        -Description "compose $Action"

    if ($Action -eq "Up") {
        Write-Success "Стенд готов. Наружу открыт только Gateway: http://localhost:10155"
        Write-Info "Логи одного сервиса: .\scripts\windows\stack.ps1 -Action Logs -Service gateway"
    }
    elseif ($Action -eq "Down") {
        Write-Success "Стенд погашен."
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
