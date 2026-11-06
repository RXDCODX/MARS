# Мутационное тестирование

В репозитории используется [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/)
для проверки того, что тесты обнаруживают изменения поведения production-кода.

## Общая конфигурация

Единый конфиг расположен в `.config/stryker-config.json`. Все проекты запускаются
с ним через `--config-file`, а target project выбирается отдельно в CI через
`--project`.

Ключевые настройки:

| Настройка | Значение | Назначение |
|---|---|---|
| `test-runner` | `mtp` | Microsoft Testing Platform для xUnit v3 |
| `coverage-analysis` | `perTest` | Запускать против мутанта только покрывающие его тесты |
| `concurrency` | `1` | Один worker Stryker на job; CI параллелит до 4 проектов |
| `mutation-level` | `Standard` | Стандартный набор операторов мутации |
| `additional-timeout` | `30000` | Дополнительный timeout тестового прогона в миллисекундах |
| `mutate` | `**/*.cs` с исключениями | Не мутировать миграции, Program.cs, generated/designer код и Protos |
| `ignore-methods` | логирование, Console, Debug, ConfigureAwait, exception constructors | Не создавать шумные мутанты на инфраструктурных вызовах |
| `reporters` | `html`, `json`, `cleartext`, `progress` | HTML/JSON-отчёт, консольная сводка и прогресс |
| `thresholds` | `80 / 60 / 0` | Пороговые зоны отчёта; `break: 0` пока не блокирует CI |
| `verbosity` | `info` | Подробность логов Stryker |

Stryker MTP runner в текущей документации upstream всё ещё обозначен как preview.
При обновлении версии нужно повторно проверить фактическое обнаружение тестов и отчёты.

## CI

Workflow `.github/workflows/mutation-tests.yml` покрывает все backend-проекты
из `src/`:

- сервисные проекты проверяются соответствующими `MARS.<Service>.Tests`;
- проекты `MARS.Shared.*` и `MARS.Platform.*.Infrastructure` проверяются
  через `MARS.Shared.Tests`, с явным выбором проекта через `--project`;
- `MARS.ClientUi.Tests` не включён: это отдельный Docker/E2E-набор, а не
  .NET unit-test project с ProjectReference на production-сборку.

На pull request и push в `main` запускается инкрементальный режим Stryker
(`--since`). Полный прогон всех проектов запускается вручную через
**Actions → Mutation Testing → Run workflow**. Еженедельного cron нет:
полный проход по 28 сборкам может быть дорогим, а автоматический инкрементальный
контроль уже выполняется на изменениях кода. Периодический полный прогон можно
вернуть отдельно, когда появится потребность регулярно пересчитывать baseline.

Каждый проект публикует отдельный artifact `stryker-<project>` с HTML- и
JSON-отчётами. Артефакты хранятся 14 дней.

### Статистика в GitHub Actions

После Stryker запускается `.github/scripts/stryker-summary.py`. Он читает
JSON-отчёт и добавляет в Step Summary:

- число обнаруженных тест-кейсов и test source files;
- сколько тестов убили хотя бы один мутант, только покрывали мутанты или не
  покрыли ни одного мутанта;
- количество мутантов в каждом состоянии: Killed, Survived, Timeout,
  NoCoverage, CompileError, RuntimeError, Ignored и Pending;
- общий mutation score и mutation score по покрытому коду;
- файлы с наибольшим числом не обнаруженных мутаций и распределение по mutation operators.

Если инкрементальный фильтр исключил все мутанты, score показывается как
`n/a`, а не как 0%: это означает, что в данном запуске не было валидных
мутантов, по которым можно оценить качество тестов.

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
  --project MARS.Platform.Messaging.RabbitMq.Infrastructure.csproj \
  --configuration Release
```

Для локального инкрементального прогона сначала обновить базовую ветку:

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
`thresholds.break = 0`. Это намеренно — сначала нужно получить baseline
для каждого проекта, а затем установить реалистичные пороги. Сами значения
`high = 80` и `low = 60` задают зоны отображения результатов.

Ошибки сборки, первоначального запуска тестов и самого Stryker по-прежнему
завершают соответствующую проверку с ошибкой.
