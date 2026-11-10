using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;

/// <summary>
/// Готовность схемы БД: позволяет IHostedService дождаться применения миграций
/// своего контекста.
/// Аудит (runtime-проверка compose): миграции выполнялись фоновым сервисом, их
/// ExecuteAsync стартовал параллельно с остальными сервисами. MARS.TwitchCore и
/// MARS.SoundRequest читали свои таблицы раньше миграций и падали с
/// 42P01: relation "twitch.RootState" does not exist → restart-loop.
/// Теперь миграции применяются синхронно до старта хоста
/// (<c>RunMarsSchemaMigrationsAsync</c>),
/// а этот интерфейс остаётся для сервисов, которые стартуют позже динамически.
/// </summary>
public interface IMarsSchemaReady<TContext>
    where TContext : DbContext
{
    Task WaitAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Применение миграций с ретраями. Идемпотентно: первый вызов выполняет миграцию,
/// последующие возвращают тот же Task, поэтому синхронный прогон перед стартом
/// хоста и фоновый сервис не дублируют работу.
/// </summary>
public sealed class MarsSchemaMigrator<TContext>(IDbContextFactory<TContext> contextFactory)
    where TContext : DbContext
{
    private const int MaxRetries = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private Task? _migrationTask;

    /// <summary>
    /// Логгер для прогонов. По умолчанию — заглушка: синхронный прогон до
    /// <c>app.Run()</c> выполняется раньше фонового сервиса, который этот
    /// логгер устанавливает. Из-за этого падение миграции оставалось невидимым:
    /// сервис стартовал с неполной схемой, и первое обращение к таблице падало
    /// с 42P01 уже в рантайме.
    /// </summary>
    public ILogger Logger { get; set; } =
        Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    public Task MigrateAsync(CancellationToken cancellationToken)
    {
        _gate.Wait(cancellationToken);

        try
        {
            _migrationTask ??= MigrateCoreAsync(cancellationToken);
            return _migrationTask;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task MigrateCoreAsync(CancellationToken cancellationToken)
    {
        var logger = ResolveLogger();
        var retryCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync(
                    cancellationToken
                );
                var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);

                var listMigrations = pending.ToList();
                if (listMigrations.Any())
                {
                    logger.LogInformation(
                        "Applying {Count} pending migrations for {ContextName}",
                        listMigrations.Count(),
                        typeof(TContext).Name
                    );
                    await context.Database.MigrateAsync(cancellationToken);
                }

                logger.LogInformation(
                    "Migrations are up to date for {ContextName}",
                    typeof(TContext).Name
                );
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount >= MaxRetries)
                {
                    // Раньше здесь был return, и это была неверная семантика
                    // для фонового вызова: MigrateAsync завершался успешно
                    // при незавершённых миграциях. Синхронный прогон до
                    // app.Run() видел «всё хорошо» и поднимал хост с
                    // неполной схемой — первое же обращение к таблице
                    // падало с 42P01 уже в рантайме. Теперь исчерпание
                    // попыток пробрасывается наружу: хост не стартует.
                    throw new InvalidOperationException(
                        $"Migrations for {typeof(TContext).Name} failed after {MaxRetries} attempts",
                        ex
                    );
                }

                logger.LogWarning(
                    ex,
                    "Failed to apply migrations for {ContextName}, retry {RetryCount}/{MaxRetryCount}",
                    typeof(TContext).Name,
                    retryCount,
                    MaxRetries
                );
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private ILogger ResolveLogger()
    {
        // Фабрика доступна только у scoped-провайдера, поэтому используется
        // NullLogger-совместимый путь: логирование ведёт вызывающий сервис.
        return Logger;
    }
}
