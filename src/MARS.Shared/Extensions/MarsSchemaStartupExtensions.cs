using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Extensions;

/// <summary>
/// Синхронное применение миграций до старта хоста.
/// Обязательный шаг: без него любой IHostedService может выполнить запрос
/// раньше, чем появится его таблица, и уронить сервис (42P01).
/// </summary>
public static class MarsSchemaStartupExtensions
{
    public static async Task RunMarsSchemaMigrationsAsync(this WebApplication app)
    {
        var migrators = app.Services.GetServices<MarsSchemaStartupMarker>().ToList();

        if (migrators.Count == 0)
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(
            typeof(MarsSchemaStartupExtensions)
        );

        foreach (var marker in migrators)
        {
            logger.LogInformation("Applying schema migrations for {Schema}", marker.SchemaName);
            await marker.MigrateAsync(app.Lifetime.ApplicationStopping);
        }
    }
}

/// <summary>Регистрация синхронного прогона миграций для конкретного контекста.</summary>
public sealed class MarsSchemaStartupMarker(string schemaName, Func<CancellationToken, Task> migrate)
{
    public string SchemaName { get; } = schemaName;

    public Task MigrateAsync(CancellationToken cancellationToken) => migrate(cancellationToken);
}
