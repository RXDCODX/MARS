using MARS.WaifuGacha.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Tests;

/// <summary>
/// Контекст WaifuGacha поверх SQLite в памяти.
///
/// Нужен там, где сервис пишет через <c>ExecuteUpdateAsync</c>: такой запрос
/// relational-only, и на провайдере InMemory он бросает исключение вместо
/// обновления — тест прошёл бы, не проверив ничего.
///
/// Соединение создаётся один раз и держится открытым: EF закрывает соединение
/// вместе с контекстом, а база <c>:memory:</c> исчезает вместе с ним.
/// </summary>
internal sealed class WaifuSqliteTestDbContextFactory
    : IDbContextFactory<WaifuDbContext>,
        IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<WaifuDbContext> _options;

    public WaifuSqliteTestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<WaifuDbContext>().UseSqlite(_connection).Options;

        using var context = new WaifuDbContext(_options);
        context.Database.EnsureCreated();
    }

    public WaifuDbContext CreateDbContext() => new(_options);

    public Task<WaifuDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(CreateDbContext());
    }

    public void Dispose() => _connection.Dispose();
}
