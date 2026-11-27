using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MARS.Telegram.Application.Services;
using MARS.Telegram.Domain.Entities;
using MARS.Telegram.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Telegram.Infrastructure.Postgres.Services;

public sealed class TelegramWhitelistService(IDbContextFactory<ChatDbContext> dbContextFactory) : ITelegramWhitelistService
{
    public async Task<IReadOnlyList<string>> GetPyroAlertsUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TelegramUsers
            .AsNoTracking()
            .Where(user => user.PyroAlertsAccess)
            .OrderBy(user => user.Name)
            .Select(user => string.IsNullOrWhiteSpace(user.Name) ? user.UserId.ToString() : user.Name)
            .ToListAsync(cancellationToken);
    }
}