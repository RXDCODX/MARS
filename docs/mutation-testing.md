# Мутационное тестирование

В репозитории используется [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/)
для проверки того, что тесты обнаруживают изменения поведения production-кода.

## Общая конфигурация

Единый конфиг расположен в `.config/stryker-config.json`. Все target-сборки
запускаются с ним через `--config-file`; проверяемая production-сборка выбирается
параметром `--project`.

| Настройка | Значение | Назначение |
|---|---|---|
| `test-runner` | `mtp` | Microsoft Testing Platform для xUnit v3 |
| `coverage-analysis` | `perTest` | Запускать против мутанта только покрывающие его тесты |
| `concurrency` | `1` | Один worker внутри каждого процесса Stryker; общий parallel runner запускает до 4 target-процессов одновременно |
| `mutation-level` | `Advanced` | Расширенный набор операторов мутации |
| `additional-timeout` | `30000` | Дополнительный timeout тестового прогона в миллисекундах |
| `mutate` | `**/*.cs` с исключениями | Не мутировать миграции, Program.cs, generated/designer код и Protos |
| `ignore-methods` | логирование, Console, Debug, ConfigureAwait, exception constructors | Исключить шумные инфраструктурные вызовы |
| `reporters` | `html`, `json`, `cleartext`, `progress` | HTML/JSON, консольная сводка и прогресс |
| `thresholds` | `80 / 60 / 0` | Пороговые зоны отчёта; `break: 0` пока не блокирует CI |
| `verbosity` | `info` | Подробность логов Stryker |

Stryker MTP runner в upstream-документации всё ещё обозначен как preview.
При обновлении версии нужно проверять обнаружение тестов и отчёты.

## CI и порядок выполнения

Мутационное тестирование встроено в `.github/workflows/ci.yml`, а не запускается
отдельным workflow:

1. Сначала запускаются `build` и матрица обычных backend-тестов (`tests`).
2. После их успешного завершения параллельно запускаются `mutation-testing`
   и `tests-coverage` (`coverage`). Оба job используют один и тот же успешный
   baseline обычных backend-тестов, но не ждут завершения друг друга.
   Мутационное тестирование зависит только от `build` и `tests`: frontend и E2E
   остаются независимыми обязательными CI-проверками, но не задерживают Stryker,
   поскольку не входят в набор target-сборок мутационного тестирования.
   Если сборка или любой backend test job завершится ошибкой, Stryker будет
   пропущен.
3. Внутри одного mutation job все 28 target-сборок отправляются в
   асинхронный bounded worker pool: одновременно работают до 4 процессов
   `dotnet stryker`, каждый с `concurrency = 1`. Как только один target
   завершается, его worker берёт следующий из очереди. Каждый worker работает
   в собственной изолированной Git worktree одного и того же commit SHA.
   Поэтому даже 11 target-сборок, использующих общий `MARS.Shared.Tests`,
   могут выполняться одновременно: их MSBuild не делят каталоги `obj/bin`.
   Worktree создаются один раз на worker, а не на каждый target. Каждый target
   пишет в отдельный каталог `stryker/<target>`, а orchestrator собирает все
   exit codes в единый `status.tsv`.
4. Job `ci-summary` запускается всегда, даже если тесты упали или mutation job
   был пропущен. Он объединяет TRX обычных тестов и JSON-отчёты Stryker, пишет
   полный отчёт в GitHub Actions Step Summary и публикует artifact `ci-summary`.

На pull request и push в `main` используется инкрементальный режим Stryker
(`--since`). Ручной запуск **Actions → CI → Run workflow** выполняет полный
прогон без `--since`. Еженедельного cron нет.

Все отчёты Stryker сохраняются одним artifact `stryker-results` на 14 дней.
Обычные TRX/Cobertura остаются в существующих `coverage-<test-project>` artifacts.

## PR-комментарий с результатами CI

После завершения обычных тестов, мутационного тестирования и остальных job workflow
обновляет один комментарий GitHub Actions bot в PR. Комментарий помечен
`<!-- MARS-CI-SUMMARY -->`: при следующем запуске workflow bot находит свой
комментарий и редактирует его вместо создания нового.

Для создания/обновления conversation comment job получает только
`issues: write`; остальные jobs сохраняют минимальные permissions.
Комментарии публикуются для PR из этого же репозитория. PR из fork и Dependabot
пропускаются: GitHub ограничивает их `GITHUB_TOKEN` read-only на событии
`pull_request`. Workflow не использует привилегированный `pull_request_target`
и не запускает код PR с write-token.

## Состав сводки

Обычные тесты читаются из TRX, создаваемого `--report-xunit-trx`:

- количество test cases, executed, passed, failed, errors, timeouts и skipped;
- результат каждой из 18 test-project jobs;
- имена и сообщения упавших тестов (до 50 записей).

Для Stryker сводка показывает:

- все 28 target-сборок и соответствующие test projects;
- обнаруженные test cases в каждом target run и тесты, которые убили мутанты,
  только покрывали их или не покрыли ни одного;
- Killed, Survived, Timeout, NoCoverage, CompileError, RuntimeError, Ignored,
  Pending;
- агрегированный mutation score, рассчитанный по сумме мутантов, а не среднему
  арифметическому score отдельных сборок.

У 11 общих target-сборок повторно используется `MARS.Shared.Tests`, поэтому
сумма test cases в mutation-таблице — это количество test-case instances по
target-run, а не число уникальных тестов. Уникальная статистика обычных тестов
берётся отдельно из матрицы TRX.

Если валидных мутантов нет, score показывается как `n/a`, а не как 0%.

## Локальный запуск

Из корня репозитория:

```bash
dotnet tool restore
```

Для сервиса:

```bash
cd tests/MARS.SoundRequest.Tests
dotnet stryker \
  --config-file ../../.config/stryker-config.json \
  --project MARS.SoundRequest.csproj \
  --configuration Release
```

Для общей инфраструктурной сборки:

```bash
cd tests/MARS.Shared.Tests
dotnet stryker \
  --config-file ../../.config/stryker-config.json \
  --project MARS.Platform.Infrastructure.Messaging.RabbitMq.csproj \
  --configuration Release
```

Для локального инкрементального прогона:

```bash
git fetch origin main
dotnet stryker \
  --config-file ../../.config/stryker-config.json \
  --since:origin/main \
  --project MARS.SoundRequest.csproj \
  --configuration Release
```

## Порог mutation score

На первом этапе CI **не блокирует слияние по mutation score**:
`thresholds.break = 0`. Сначала нужно получить baseline для каждого проекта,
а затем установить реалистичные пороги. Значения `high = 80` и `low = 60`
задают зоны отображения результатов.

Ошибки сборки, первоначального запуска тестов и самого Stryker по-прежнему
завершают соответствующий mutation job с ошибкой.
