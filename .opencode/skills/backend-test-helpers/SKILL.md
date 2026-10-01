---
name: backend-test-helpers
description: Conventions for writing .NET tests in the MARS microservices repo — choosing an EF provider, reusing fixtures, passing CancellationToken, and avoiding network access. Use when adding or fixing any test under tests/.
---

# Backend Test Helpers (MARS)

Используй этот навык при написании **любых** тестов в `tests/MARS.X.Tests` — для
схемы БД, сервисной логики, RabbitMQ-потребителей и файлового хранилища.

## Главное правило: тест не ходит наружу

Ни сети, ни поднятого docker-стека, ни реального git. Всё, что наружу, заменяется
заглушкой в самом тест-проекте. Тест, требующий `docker compose up`, в этом
репозитории считается негодным.

## Провайдер EF: InMemory против SQLite

| Задача | Провайдер | Пример в репозитории |
|---|---|---|
| Схема: имена таблиц, `HasDefaultSchema`, nullability, `ValueGenerated` | `Microsoft.EntityFrameworkCore.InMemory` | `AdhdLayoutConfigSchemaTests`, `BooruSchemaTests`, `Videos365ModelTests` |
| Реальные уникальные индексы, транзакции, каскады | `Microsoft.EntityFrameworkCore.Sqlite` (in-memory, удерживаемое открытым соединением) | `StorageTestContext` в `MARS.MediaStorage.Tests` |

InMemory — выбор по умолчанию: дешевле и не тянет нативную библиотеку.
**SQLitePCLRaw не используется без нужды**: он тянет нативный код с
CVE-2025-6965, исправленной версии пакета не существует (GHSA-2m69-gcr7-jv3q).
В тестовых провайдерах нативный код не участвует — риск нулевой, а пакет в графе
зависимостей остаётся.

Комментарий в `.csproj` тестового проекта объясняет выбор — не удаляй его молча,
проверь, что он всё ещё верен.

### InMemory: важные подводные камни

- `UseInMemoryDatabase("test")` даёт **общую базу для всех тестов с этим именем**.
  Давай уникальное имя (`nameof(Метод)`), иначе тесты будут видеть данные друг друга.
- InMemory не проверяет SQL. Запрос, который на PostgreSQL упал бы из-за
  неподдерживаемого оператора, здесь пройдёт. Такие случаи — это тест на SQLite.
- `EF.Functions.ILike`, raw SQL и `HasDefaultSchema` — relational-only. В InMemory
  они либо не сработают, либо промолчат.

### SQLite: соединение должно жить

EF закрывает соединение при `Dispose` контекста, и `:memory:`-база исчезает вместе
с ним. Поэтому в `StorageTestContext` соединение создаётся один раз, открывается и
передаётся в `DbContextOptions`:

```csharp
_connection = new SqliteConnection("Data Source=:memory:");
_connection.Open();

var options = new DbContextOptionsBuilder<MediaStorageDbContext>()
    .UseSqlite(_connection)
    .Options;
```

Также нужен `db.Database.EnsureCreated()` — миграции в тестах не применяются.

## Готовые фикстуры — не изобретай свои

| Фикстура | Где | Что даёт |
|---|---|---|
| `StorageTestContext` | `tests/MARS.MediaStorage.Tests/StorageTestContext.cs` | temp-каталог + SQLite in-memory + `RecordingGitService` + `FakeTimeProvider` |
| `TestDbContextFactory` | там же | `IDbContextFactory<T>` поверх готовых опций |
| `RecordingGitService` | там же | считает коммиты, не ходит в сеть |
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

Время в тестах — **только** через `FakeTimeProvider`. `DateTime.Now`/`UtcNow` внутри
сервиса делает тест зависимым от часов машины; эталон — `StorageTestContext.Now`
как фиксированная точка.

## CancellationToken — обязателен

xunit.v3 даёт токен теста. Во всех асинхронных вызовах передавай
`TestContext.Current.CancellationToken` — иначе тест не отменится по таймауту и
заблокирует прогон:

```csharp
var ct = TestContext.Current.CancellationToken;
await service.DoAsync(ct);
```

Эталон: `tests/MARS.Shared.Tests/Concurrency/KeyedAsyncLockTests.cs`,
`tests/MARS.Shared.Tests/HealthCheckConnectionTests.cs`.

## Smoke-тест в каждом проекте

Каждый из 16 тестовых проектов содержит `SmokeTests.cs`: он грузит сборку сервиса
и падает, если проект-ссылка потерялась. Пустой тест-проект иначе собрался бы
успешно, проверяя себя и ничего.

## Комментарии в тестах — почему, а не что

Комментарий оправдан там, где тест фиксирует **решение**, а не механику:
почему `Id` не генерируется, почему дата берётся из момента приёма, а не из mtime
файла. Пересказывание кода — мусор, который устаревает первым. Эталон:
`AdhdLayoutConfigSchemaTests`.

## Перед сдачей

```bash
dotnet test tests/MARS.X.Tests/MARS.X.Tests.csproj -c Release   # см. dotnet-test-run про --filter
dotnet csharpier tests/MARS.X.Tests/НовыйТест.cs
```

Новый тестовый проект ⇒ добавить в `MARS.slnx` (папка `/tests/`) **и** в матрицу
`tests` в `.github/workflows/ci.yml`, иначе он не проверяется вовсе.