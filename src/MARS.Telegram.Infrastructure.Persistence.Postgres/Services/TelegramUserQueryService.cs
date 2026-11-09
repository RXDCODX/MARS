using MARS.Telegram.Application.Services;
using MARS.Telegram.Infrastructure.Persistence.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Infrastructure.Persistence.Postgres.Services;

/// <summary>EF Core реализация запросов к пользователям Telegram.</summary>
public sealed class TelegramUserQueryService(IDbContextFactory<ChatDbContext> dbFactory)
    : ITelegramUserQueryService
{
    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.TelegramUsers.AsNoTracking().CountAsync(cancellationToken);
    }
}
