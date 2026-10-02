using MARS.Telegram.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Tests;

/// <summary>
/// Фабрика контекстов чата в памяти.
///
/// Имя базы уникально на экземпляр: у <c>UseInMemoryDatabase</c> база с одним
/// именем общая для всего процесса, и тесты видели бы сессии друг друга.
/// </summary>
internal sealed class ChatTestDbContextFactory : IDbContextFactory<ChatDbContext>
{
    private readonly DbContextOptions<ChatDbContext> _options;

    public ChatTestDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase($"chat-{Guid.NewGuid():N}")
            .Options;
    }

    public ChatDbContext CreateDbContext() => new(_options);

    public async Task<ChatDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default
    )
    {
        var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return context;
    }
}
