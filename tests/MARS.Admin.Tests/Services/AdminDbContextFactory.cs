using MARS.Admin.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Контекст админки поверх провайдера в памяти: проверяется логика сервисов, а не
/// PostgreSQL. Схему создаёт сам провайдер, внешняя база не нужна.
/// </summary>
internal sealed class AdminDbContextFactory : IDbContextFactory<AdminDbContext>
{
    private readonly DbContextOptions<AdminDbContext> _options;

    public AdminDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase($"admin-{Guid.NewGuid()}")
            .Options;
    }

    public AdminDbContext CreateDbContext() => new(_options);

    public Task<AdminDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(CreateDbContext());
}
