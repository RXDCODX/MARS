# Мутационное тестирование

В репозитории используется [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/)
для проверки того, что тесты обнаруживают изменения поведения production-кода.

## Интеграция

Версия инструмента закреплена в `.config/dotnet-tools.json` — **dotnet-stryker 5.0.0**.
Она требует .NET 10 Runtime и подходит к SDK из `global.json`.

Тестовые проекты используют xUnit v3 и Microsoft Testing Platform. Поэтому Stryker
запускается с `--test-runner mtp`, а не с VSTest. MTP runner в Stryker всё ещё
обозначен upstream-документацией как preview; при обновлении Stryker нужно
проверять отчёты и фактическое обнаружение тестов.

Workflow `.github/workflows/mutation-tests.yml` покрывает все backend-проекты
из `src/`:

- сервисные проекты проверяются соответствующими `MARS.<Service>.Tests`;
- проекты `MARS.Shared.*` и `MARS.Platform.*.Infrastructure` проверяются
  через `MARS.Shared.Tests`, с явным выбором проекта через `--project`;
- `MARS.ClientUi.Tests` не включён: это отдельный Docker/E2E-набор, а не
  .NET unit-test project с ProjectReference на production-сборку.

На pull request и push в `main` используется инкрементальный режим Stryker
(`--since`): мутируются изменённые участки production-кода и участки,
результат которых затрагивают изменённые тесты. Полный прогон всех проектов
запускается еженедельно (понедельник, 02:00 UTC) и вручную через
**Actions → Mutation Testing → Run workflow**.

Каждый проект публикует отдельный artifact `stryker-<project>` с HTML- и
JSON-отчётами. Артефакты хранятся 14 дней.

## Локальный запуск

Восстановить локальные инструменты из корня репозитория:

```bash
dotnet tool restore
```

Запустить мутационное тестирование сервиса:

```bash
cd tests/MARS.SoundRequest.Tests
dotnet stryker \
  --project MARS.SoundRequest.csproj \
  --test-runner mtp \
  --configuration Release \
  --reporter progress \
  --reporter html \
  --reporter json
```

Для общего инфраструктурного проекта команда запускается из
`tests/MARS.Shared.Tests`, а `--project` указывает имя целевого `.csproj`
из его ProjectReference, например:

```bash
cd tests/MARS.Shared.Tests
dotnet stryker \
  --project MARS.Platform.Messaging.RabbitMq.Infrastructure.csproj \
  --test-runner mtp \
  --configuration Release
```

Для локального инкрементального прогона сначала обновить базовую ветку, затем
передать её в Stryker:

```bash
git fetch origin main
dotnet stryker --since:origin/main --project MARS.SoundRequest.csproj --test-runner mtp
```

## Порог mutation score

На первом этапе CI **не блокирует слияние по mutation score**:
`--break-at 0` оставляет порог отказа выключенным. Это намеренно — у проектов
пока нет измеренного baseline, а единый произвольный порог для сервисов с разным
объёмом и зрелостью тестов создаст ложные блокировки.

Ошибки компиляции, первоначального запуска тестов и самого Stryker при этом
по-прежнему завершают соответствующую проверку с ошибкой. После нескольких
полных прогонов следует выбрать реалистичные пороги отдельно по проектам и
включить их через `--break-at`.
