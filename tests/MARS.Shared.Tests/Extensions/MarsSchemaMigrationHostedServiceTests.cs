using System.Reflection;
using MARS.Shared.Extensions;
using MARS.TestKit.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Tests.Extensions;

/// <summary>
/// Резервный фоновый прогон миграций.
///
/// Схема уже применена синхронно до старта хоста, поэтому фоновая задача должна
/// завершиться без ошибки. Ветку сбоя проверить нельзя: она вызывает
/// <see cref="Environment.Exit(int)"/> и утащила бы за собой тестовый хост.
/// </summary>
public class MarsSchemaMigrationHostedServiceTests
{
    /// <summary>
    /// Повторный прогон на уже применённой схеме завершается молча, а логгер
    /// мигратора перехватывается сервисом: иначе синхронный прогон до
    /// <c>app.Run()</c> остался бы без журнала.
    /// </summary>
    [Fact]
    public async Task BackgroundRunOnReadySchemaCompletes()
    {
        var factory = new TestContextFactory();
        await using (
            var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        var migrator = new MarsSchemaMigrator<ProbeDbContext>(factory);
        var logger = new CollectingLogger();
        var service = new MarsSchemaMigrationHostedService<ProbeDbContext>(migrator, logger);

        var execute = typeof(MarsSchemaMigrationHostedService<ProbeDbContext>).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        await (Task)execute.Invoke(service, [TestContext.Current.CancellationToken])!;

        Assert.Same(logger, migrator.Logger);
        Assert.Contains(logger.Messages, message => message.Contains("up to date"));
    }

    /// <summary>
    /// Журнал мигратора приходит из сервиса: синхронный прогон до
    /// <c>app.Run()</c> выполняется раньше, и без него падение миграции осталось бы
    /// невидимым.
    /// </summary>
    private sealed class CollectingLogger
        : ILogger<MarsSchemaMigrationHostedService<ProbeDbContext>>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add(formatter(state, exception));
    }

    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options)
        : DbContext(options);

    /// <summary>
    /// Пробный контекст поверх живой PostgreSQL: <c>GetPendingMigrationsAsync</c>
    /// требует провайдер, умеющий отдавать список миграций, и на живой базе он
    /// работает так же, как на стенде.
    /// </summary>
    private sealed class TestContextFactory : PostgresTestDbContextFactory<ProbeDbContext>;
}
