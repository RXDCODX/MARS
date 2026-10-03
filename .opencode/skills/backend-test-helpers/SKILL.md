---
name: backend-test-helpers
description: Conventions for writing .NET tests in the MARS microservices repo — PostgreSQL fixtures, reusing helpers, passing CancellationToken, and avoiding network access. Use when adding or fixing any test under tests/.
---

# Backend Test Helpers (MARS)

Используй этот навык при написании **любых** тестов в `tests/MARS.X.Tests` — для
схемы БД, сервисной логики, RabbitMQ-потребителей и файлового хранилища.

## Главное правило: тест не ходит наружу

Ни сети, ни реального git. Всё, что наружу, заменяется заглушкой в самом
тест-проекте. Тест, требующий `docker compose up` со стендом, считается
негодным.

**Исключение одно, и оно обязательное**: база. Любая проверка работы с БД идёт
против PostgreSQL, который поднимает Testcontainers. Отдельной базы в
репозитории нет — ни в compose, ни в `.devcontainer`; контейнер создаёт
`tests/MARS.TestKit/Postgres/MarsPostgres.cs` при первом обращении к базе и
убирается сам (Ryuk).

## Провайдер EF: только PostgreSQL

`UseInMemoryDatabase`, `UseSqlite` и `SqliteConnection` в `tests/` не
используются, и пакеты `Microsoft.EntityFrameworkCore.InMemory`/`.Sqlite`
вычищены из проектов. Причина не в «строгости»: обходные провайдеры проверяли
собранную модель, а не работу с базой. При переводе это стоило трёх
production-дефектов (запись `DateTime` с `Kind=Local` в `timestamptz`,
смешение UTC и локального времени, счётчик, возвращавший `SaveChangesAsync`).

### Фабрика: наследуй общую

```csharp
internal sealed class WaifuTestDbContextFactory
    : PostgresTestDbContextFactory<WaifuDbContext>;
```

`PostgresTestDbContextFactory<TContext>` даёт свою базу на экземпляр (то есть на
тест, потому что xUnit создаёт класс на каждый тест) и применяет **настоящие
миграции** сервиса. `EnsureCreated` не нужен и вреден: он строит схему из модели,
а не из миграций, и расхождение с production всплыло бы только на развёртывании.

- `Options` — если тест создаёт контекст сам, а не через фабрику.
- База освобождается через `Dispose()`/`DisposeAsync()`. Класс теста
  реализует `IDisposable` и вызывает фабрику — иначе базы копятся до конца
  прогона.
- Контексты без миграций (тестовые пробы в `MARS.Shared.Tests`) фабрика
  создаёт через `EnsureCreated` сама: `HasMigrations` проверяется на лету.

### Первый контекст платит за контейнер

Подъём postgres занимает несколько секунд, и он происходит лениво. Тест, который
запускает фоновой цикл и ждёт его первый шаг, обязан сначала прогреть фабрику,
иначе окно ожидания пройдёт до старта контейнера:

```csharp
await using var _ = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
```

### Даты

Всё, что пишется в базу, приводится к UTC конвертером `MarsUtcDates`. Значение,
прочитанное из базы, приходит с `Kind=Utc` — печатаемое время зрителю зовёт
`ToLocalTime()` сам. Для времени в тестах есть `FakeTimeProvider`.

## Готовые фикстуры — не изобретай свои

| Фикстура | Где | Что даёт |
|---|---|---|
| `PostgresTestDbContextFactory<T>` | `tests/MARS.TestKit/Postgres/` | своя база на тест + миграции сервиса |
| `StorageTestContext` | `tests/MARS.MediaStorage.Tests/StorageTestContext.cs` | temp-каталог + PostgreSQL + `RecordingGitService` + `FakeTimeProvider` |
| `RecordingGitService` | там же | считает коммиты, не ходит в git |
| `FakeTimeProvider` | там же | управляемое `UtcNow`, `Advance(delta)` |

Использование:

```csharp
using var ctx = new StorageTestContext();
await ctx.CreateService().UploadAsync([MakeFile("clip.mp4", "x")], "Uploads");

Assert.True(ctx.Exists("Uploads/clip.mp4"));
Assert.Equal(1, ctx.Git.Messages.Count);
```

Если нужен другой сервис — пиши заглушку по образцу `RecordingGitService`:
интерфейс реализуется полностью, поведение считается, сети нет.

## CancellationToken — обязателен

xunit.v3 даёт токен теста. Во всех асинхронных вызовах передавай
`TestContext.Current.CancellationToken` — иначе тест не отменится по таймауту и
заблокирует прогон:

```csharp
var ct = TestContext.Current.CancellationToken;
await service.DoAsync(ct);
```

Если токен в сигнатуре не последний, передавай его именованным аргументом:
`ListAsync(cancellationToken: ct)`.

Эталон: `tests/MARS.Shared.Tests/Concurrency/KeyedAsyncLockTests.cs`,
`tests/MARS.Shared.Tests/HealthCheckConnectionTests.cs`.

## Smoke-тест в каждом проекте

Каждый из 17 тестовых проектов содержит `SmokeTests.cs`: он грузит сборку сервиса
и падает, если проект-ссылка потерялась. Пустой тест-проект иначе собрался бы
успешно, проверяя себя и ничего.

## Комментарии в тестах — почему, а не что

Комментарий оправдан там, где тест фиксирует **решение**, а не механику: почему
база своя на тест, почему дата берётся из момента приёма, а не из mtime файла.
Пересказывание кода — мусор, который устаревает первым. Эталон:
`AdhdLayoutConfigSchemaTests`.

## Пути на разных ОС

Тесты гоняются на `ubuntu-latest` в CI, поэтому виндовые пути в тестовых данных
недопустимы: собирай их через `Path.Combine`, а не константой вида
`C:\Alerts\_converted\...`. То же с набором запрещённых символов имени файла —
он берётся из константы `ForbiddenFileNameChars`, а не из
`Path.GetInvalidFileNameChars()`, который на Linux знает только про NUL и «/».

## Перед сдачей

```bash
dotnet test tests/MARS.X.Tests/MARS.X.Tests.csproj -c Release   # фильтры: см. dotnet-test-run
dotnet csharpier format tests/MARS.X.Tests/НовыйТест.cs
```

Тесту с базой нужен Docker: локально Docker Desktop, в CI раннер GitHub.

Новый тестовый проект ⇒ добавить в `MARS.slnx` (папка `/tests/`) **и** в матрицу
`tests` в `.github/workflows/ci.yml`, иначе он не проверяется вовсе.