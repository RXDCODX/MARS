---
name: dotnet-test-run
description: Run .NET tests in the MARS microservices repo with Microsoft.Testing.Platform filtering, build verification, and localized result parsing. Use when the user asks to run tests or verify a change.
---

# .NET Test Run (MARS, xunit.v3 + Microsoft.Testing.Platform)

Тестовые проекты используют **Microsoft.Testing.Platform** (включён в `global.json`), а не VSTest.

## Главная ловушка: `--filter` не работает

VSTest-выражение `--filter "FullyQualifiedName~X"` здесь может молча найти ноль тестов. Фильтры MTP передаются после разделителя `--`:

```bash
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- --filter-class "*HealthCheck*"
dotnet test ... -- --filter-method "*Namespace.Class.Method"
dotnet test ... -- --filter-namespace "*Media*"
dotnet test ... -- --filter-trait "key=value"
```

## Workflow

```bash
# собрать проект
dotnet build tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release

# все тесты проекта
dotnet test tests/MARS.MediaStorage.Tests/MARS.MediaStorage.Tests.csproj -c Release

# один класс
dotnet test tests/MARS.Shared.Tests/MARS.Shared.Tests.csproj -c Release -- --filter-class "*OpenTelemetryPrometheusBridge*"

# подробный вывод
dotnet test tests/MARS.CinemaQueue.Tests/MARS.CinemaQueue.Tests.csproj -c Release -- --output Detailed
```

Ошибки компиляции исправляй до анализа тестов. В корневом `Directory.Build.props` включён `TreatWarningsAsErrors`: предупреждения компилятора тоже могут ломать сборку.

## Разбор результата

Вывод локализован:

| Что искать | Значит |
|---|---|
| `итог` | итоговая строка |
| `успешно` | число прошедших |
| `сбой` | число упавших |
| `пропущено` | число пропущенных |
| `Пройден!` | всё зелёное |
| код 8 / ноль тестов | проверь синтаксис фильтра и имя класса |

Код выхода 0 означает успех; ненулевой — ошибка тестов, сборки или инфраструктуры. Если MTP неожиданно обнаруживает ноль тестов при успешной сборке, проверь интеграцию SDK/runner и фактический запуск тестового DLL, а не делай вывод по одной строке вывода.

## Покрытие

Не собирай собственный набор команд для CI-equivalent coverage. Используй skill `mars-coverage` и канонический `.github/scripts/coverage-local.ps1`. Важные детали:

- Coverlet для MTP — `coverlet.MTP`; VSTest collectors несовместимы.
- `--coverlet-include "[MARS.*]*"` обязателен, чтобы не включать сторонние сборки.
- Сводный отчёт строится слиянием Cobertura через ReportGenerator; складывать проценты/числа проектов нельзя.
- CI coverage запускается только после успешной матрицы `tests`; актуальный порог задаётся в `.github/workflows/ci.yml`.

## Форматирование

Локально форматируй до коммита: auto-format workflow не заменяет форматирование feature-ветки.

```bash
dotnet csharpier format tests/MARS.Shared.Tests/Concurrency/KeyedAsyncLockTests.cs
dotnet csharpier check .
```

## Частые классы

| Что | Проект | Фильтр |
|---|---|---|
| Мост метрик → prometheus-net | `MARS.Shared.Tests` | `--filter-class "*OpenTelemetryPrometheusBridge*"` |
| Health-checks | `MARS.Shared.Tests` | `--filter-class "*HealthCheck*"` |
| Карта Swagger | `MARS.Gateway.Tests` | `--filter-class "*SwaggerEndpointMap*"` |
| Git-слой хранилища | `MARS.MediaStorage.Tests` | `--filter-class "*MediaGit*"` |
| Индекс хранилища | `MARS.MediaStorage.Tests` | `--filter-class "*Index*"` |

Для PostgreSQL fixtures, Testcontainers, Moq и CancellationToken читай `backend-test-helpers`.
