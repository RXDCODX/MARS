---
name: dotnet-test-run
description: Run .NET tests in the MARS microservices repo with Microsoft.Testing.Platform filtering, build verification, coverage, and localized result parsing. Use when the user asks to run tests, verify tests pass, or measure coverage.
---

# .NET Test Run (MARS, xunit.v3 + Microsoft.Testing.Platform)

16 тестовых проектов, по одному на сервис. Прогон через `dotnet test` работает поверх
**Microsoft.Testing.Platform** (включено в `global.json`), а не поверх VSTest.

## Главная ловушка: `--filter` не работает

VSTest-выражение `--filter "FullyQualifiedName~X"` на этих проектах **молча находит
ноль тестов** и выходит с кодом 8. Это не «тестов нет», это неверный синтаксис.

```bash
# НЕЛЬЗЯ (находит 0 тестов, код 8):
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj --filter "FullyQualifiedName~Health"

# ПРАВИЛЬНО (опции тест-приложения — после `--`):
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-class "*HealthCheck*"
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-method "*Namespace.Class.Method"
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-namespace "*Media*"
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -- --filter-trait "key=value"
```

## Workflow

### 1. Собрать (опции сборки — до `--`)

```bash
dotnet build tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release --no-restore
```

Ошибки `CS…` чинить до прогона тестов. `TreatWarningsAsErrors` выключен, поэтому
предупреждения сборку не роняют — но новые лучше убрать.

### 2. Прогнать

```bash
# все тесты сервиса
dotnet test tests/MARS.MediaStorage.Tests/MARS.MediaStorage.Tests.csproj -c Release

# один класс
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- --filter-class "*OpenTelemetryPrometheusBridge*"

# подробный вывод при падении
dotnet test tests/MARS.CinemaQueue.Tests/MARS.CinemaQueue.Tests.csproj -c Release -- --output Detailed
```

### 3. Разобрать результат

Вывод **локализован**:

| Что искать | Значит |
|---|---|
| `итог` | итоговая строка |
| `успешно` | число прошедших |
| `сбой` | число упавших |
| `пропущено` | число пропущенных |
| `Пройден!` | всё зелёное |
| код 8 | ноль найденных тестов — почти всегда неверный фильтр |

Код выхода: 0 — все прошли, иначе есть падения. Число тестов равно нулю при верном
фильтре — повод проверить имя класса, а не искать баг в сервисе.

### 4. Покрытие одного проекта

```bash
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- \
  --coverlet --coverlet-output-format cobertura \
  --coverlet-include "[MARS.*]*" --coverlet-exclude "[*.Tests]*" \
  --coverlet-exclude-by-file "**/Migrations/**"
```

- `--coverlet-include "[MARS.*]*"` **обязателен**: без него coverlet берёт чужие
  сборки (HealthChecks, YARP, Serilog) и отчёт распухает до тысяч строк.
- Отчёты по проектам **нельзя складывать**: `MARS.Shared` инструментируется в каждом
  тестовом проекте и посчитался бы 16 раз. Слияние — ReportGenerator:
  ```bash
  dotnet reportgenerator -reports:"out1;out2" -targetdir:coverage-report
  python .github/scripts/coverage-gate.py --merged coverage-report/Cobertura.xml --threshold-methods 95
  ```
- Пустой набор отчётов → ошибка gate, а не «0%».
- Задача `coverage` в CI падает всегда (порог 95%, факт ~4.7%). Это задуманный
  ориентир, а не поломка: `tests` и `build` остаются зелёными.

## Частые классы

| Что | Проект | Фильтр |
|---|---|---|
| Мост метрик → prometheus-net | `MARS.Shared.Tests` | `--filter-class "*OpenTelemetryPrometheusBridge*"` |
| Health-checks | `MARS.Shared.Tests` | `--filter-class "*HealthCheck*"` |
| Карта Swagger | `MARS.Gateway.Tests` | `--filter-class "*SwaggerEndpointMap*"` |
| Git-слой хранилища | `MARS.MediaStorage.Tests` | `--filter-class "*MediaGit*"` |
| Индекс хранилища | `MARS.MediaStorage.Tests` | `--filter-class "*Index*"` |

## Форматирование

`dotnet csharpier` — автоформат, в CI он коммитится отдельным workflow'ом. Локально
прогоняй до коммита, чтобы не плодить лишний `style:`-коммит:

```bash
dotnet csharpier tests/MARS.Shared.Tests/Concurrency/KeyedAsyncLockTests.cs
dotnet csharpier --check .
```

## Notes

- `--configuration Release` — как в CI; в Debug расхождения с матрицей видны как «зелёные
  локально, красные в CI».
- `--no-restore` ускоряет повторные прогоны; перед первым нужен restore.
- Тесты не ходят в сеть и не требуют поднятого стека: RabbitMQ, БД и git замоканы
  (см. `backend-test-helpers`).