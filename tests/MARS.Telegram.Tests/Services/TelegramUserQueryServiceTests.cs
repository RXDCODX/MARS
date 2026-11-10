using MARS.Telegram.Domain.Entities;
using MARS.Telegram.Infrastructure.Postgres.Services;

namespace MARS.Telegram.Tests.Services;

/// <summary>Проверяет Postgres-адаптер запроса пользователей Telegram.</summary>
public class TelegramUserQueryServiceTests : IDisposable
{
    private readonly ChatTestDbContextFactory _factory = new();

    [Fact]
    public async Task ReturnsCountOfPersistedUsers()
    {
        await using (var db = await _factory.CreateDbContextAsync(Token))
        {
            db.TelegramUsers.Add(new TelegramUser { Name = "test-user", UserId = 123456789 });
            await db.SaveChangesAsync(Token);
        }

        var service = new TelegramUserQueryService(_factory);

        Assert.Equal(1, await service.GetUserCountAsync(Token));
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;
}
