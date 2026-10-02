using MARS.TwitchCore.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Контекст Twitch в памяти: проверяются запросы и сохранение, а не PostgreSQL.
/// </summary>
internal sealed class TwitchTestDbContextFactory : IDbContextFactory<TwitchDbContext>
{
    private readonly DbContextOptions<TwitchDbContext> _options;

    public TwitchTestDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<TwitchDbContext>()
            .UseInMemoryDatabase($"twitch-{Guid.NewGuid():N}")
            .Options;
    }

    public TwitchDbContext CreateDbContext() => new(_options);

    public async Task<TwitchDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default
    )
    {
        var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return context;
    }
}
