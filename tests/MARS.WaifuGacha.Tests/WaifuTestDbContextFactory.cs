using MARS.WaifuGacha.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Tests;

/// <summary>
/// Контекст WaifuGacha в памяти: проверяются запросы и сохранение, а не PostgreSQL.
///
/// Имя базы уникально на каждый экземпляр: у <c>UseInMemoryDatabase</c> база с
/// одним именем общая для всех контекстов процесса, и тесты видели бы данные
/// друг друга.
/// </summary>
internal sealed class WaifuTestDbContextFactory : IDbContextFactory<WaifuDbContext>
{
    private readonly DbContextOptions<WaifuDbContext> _options;

    public WaifuTestDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<WaifuDbContext>()
            .UseInMemoryDatabase($"waifu-{Guid.NewGuid():N}")
            .Options;
    }

    public WaifuDbContext CreateDbContext() => new(_options);

    public async Task<WaifuDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default
    )
    {
        var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return context;
    }
}
