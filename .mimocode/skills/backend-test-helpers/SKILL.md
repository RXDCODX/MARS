# Backend Test Helpers

Используй этот навык при написании **любых** backend-тестов в MARS.Tests — для Twitch-сервисов, логов, звуковых запросов и любого другого кода, работающего с БД или внешними зависимостями.

## Правила

1. **Для тестов с relational БД** — используй `PostgresFixture` (xUnit collection fixture), **не** InMemory провайдер
2. **Для тестов Twitch-сервисов** — используй `TwitchTestHelper` и `ValidationServiceMockHelper`
3. **НИКОГДА** не дублируй reflection-хелперы (`SetMemberValue`, `CreateUninitialized`, `SetBackingField`) — они уже есть в `TwitchTestHelper`
4. **НИКОГДА** не создавай `TokenService` через `new TokenService(...)` без передачи `dbFactory`

## Когда использовать InMemory vs PostgreSQL

| Сценарий | Провайдер | Пример |
|----------|-----------|--------|
| Запросы только с LINQ (Where, OrderBy, GroupBy) | InMemory | Большинство тестов Twitch-сервисов |
| `HasDefaultSchema`, `EF.Functions.ILike`, raw SQL | PostgreSQL | `LoggerDbContext`, `AppDbContext` с ILike |
| Миграции, `Database.Migrate()` | PostgreSQL | Тесты миграций |
| Foreign keys, каскадные удаления | PostgreSQL | `SoundRequestUserQueueRelationalTests` |

**Правило:** Если контекст использует что-то из `Microsoft.EntityFrameworkCore.Relational` в `OnModelCreating` или конструкторе — нужен PostgreSQL.

## PostgresFixture (MARS.Tests/Helpers/PostgresFixture.cs)

xUnit `IAsyncLifetime` fixture, предоставляющий реальную PostgreSQL базу.

**Поведение:**
- **CI (Docker доступен):** запускает TestContainers PostgreSQL контейнер (`postgres:16-alpine`)
- **Локально (Docker недоступен):** подключается к локальному PostgreSQL (`localhost:5432`, `postgres/postgres`)
- **Ни то, ни другое:** `IsAvailable = false`, тесты пропускаются через `Assert.Skip()`

**Использование:**
```csharp
[Collection("PostgresTests")]
public class MyTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        if (!fixture.IsAvailable)
            return;

        await fixture.EnsureLoggerSchemaCreatedAsync(); // или EnsureAppSchemaCreatedAsync()
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SomeTest()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("PostgreSQL not available");
            return;
        }

        await using var context = fixture.CreateLoggerDbContext();
        // ... test logic ...
    }
}
```

**Методы:**
- `CreateLoggerDbContext()` — контекст для логов (`LoggerDbContext`)
- `CreateAppDbContext()` — основной контекст (`AppDbContext`)
- `EnsureLoggerSchemaCreatedAsync()` — создать схему для LoggerDbContext
- `EnsureAppSchemaCreatedAsync()` — создать схему для AppDbContext
- `IsAvailable` — доступна ли PostgreSQL

## TwitchTestHelper (MARS.Tests/TwitchTestHelper.cs)

Централизованные хелперы для Twitch-тестов.

**Reflection:**
```csharp
TwitchTestHelper.CreateUninitialized<T>()
TwitchTestHelper.SetMemberValue(target, name, val)
TwitchTestHelper.SetBackingField(obj, name, val)
TwitchTestHelper.GetMemberValue(target, name)
```

**Twitch event args:**
```csharp
var args = TwitchTestHelper.CreateRedemptionArgs(userId, userName, cost: 2);
var args = TwitchTestHelper.CreateMessageArgs(channel, username, userId);
```

**ITwitchClient mock:**
```csharp
var clientMock = TwitchTestHelper.CreateTwitchClientMock();
var clientMock = TwitchTestHelper.CreateTwitchClientMockWithSend();
```

**TokenService:**
```csharp
var tokenService = TwitchTestHelper.CreateUninitializedTokenService();
var tokenService = TwitchTestHelper.CreateTokenService(dbFactory);
var tokenService = TwitchTestHelper.CreateTokenServiceWithToken(tokenInfo);
```

**DB Factory (InMemory):**
```csharp
var dbFactory = TwitchTestHelper.CreateDbFactory(); // InMemory, для Twitch-тестов
await TwitchTestHelper.SeedTwitchUserAsync(dbFactory, twitchId, displayName);
```

## ValidationServiceMockHelper (MARS.Tests/ValidationServiceMockHelper.cs)

```csharp
var validator = ValidationServiceMockHelper.CreatePassingValidator();
var validator = ValidationServiceMockHelper.CreateFailingValidator();
```

## Известные проблемы

### EF Functions.ILike не работает с InMemory

`EF.Functions.ILike()` (PostgreSQL-specific) не поддерживается InMemory провайдером.
**Решение:** Используй `PostgresFixture` для тестов, зависящих от `ILike`.

### HasDefaultSchema не работает с InMemory

`builder.HasDefaultSchema("logs")` в `LoggerDbContext.OnModelCreating` — relational-only.
**Решение:** Используй `PostgresFixture` для тестов с `LoggerDbContext`.

### Moq не может проксировать конкретные классы

Используй `RuntimeHelpers.GetUninitializedObject` (через `TwitchTestHelper.CreateUninitialized*()`) или реальные конструкторы с mock-зависимостями.

### TwitchLib readonly nested объекты

TwitchLib типы имеют readonly свойства. Единственный способ создания — через reflection.
**Решение:** Используй `TwitchTestHelper.CreateRedemptionArgs()`.
