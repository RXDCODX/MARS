using MARS.Shikimori.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Shikimori.Tests;

/// <summary>
/// Контекст Shikimori в памяти.
///
/// Имя базы уникально на экземпляр: у <c>UseInMemoryDatabase</c> база с одним
/// именем общая для всего процесса, и тесты видели бы персонажей друг друга.
/// </summary>
internal sealed class ShikimoriTestDbContextFactory : IDbContextFactory<ShikimoriDbContext>
{
    private readonly DbContextOptions<ShikimoriDbContext> _options;

    public ShikimoriTestDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<ShikimoriDbContext>()
            .UseInMemoryDatabase($"shikimori-{Guid.NewGuid():N}")
            .Options;
    }

    public ShikimoriDbContext CreateDbContext() => new(_options);

    public async Task<ShikimoriDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default
    )
    {
        var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return context;
    }
}
