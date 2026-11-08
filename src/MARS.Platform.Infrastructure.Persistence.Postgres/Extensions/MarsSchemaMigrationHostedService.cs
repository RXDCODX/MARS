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
/// Резервный фоновый прогон миграций. Схема уже применена синхронно до старта
/// хоста, поэтому здесь задача обычно завершается мгновенно; сервис оставлен,
/// чтобы миграции применялись и при смене конфигурации/перезапуске.
/// </summary>
public class MarsSchemaMigrationHostedService<TContext>(
    MarsSchemaMigrator<TContext> migrator,
    ILogger<MarsSchemaMigrationHostedService<TContext>> logger
) : BackgroundService
    where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        migrator.Logger = logger;

        try
        {
            await migrator.MigrateAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Штатная остановка хоста, пока миграция ждёт ретрай.
        }
        catch (Exception ex)
        {
            // Здесь ошибку логируем и завершаемся с ненулевым кодом, а не
            // проглатываем. Схема к этому моменту уже применена синхронно
            // до app.Run(), поэтому падение здесь означает расхождение между
            // синхронным и фоновым прогоном — тихо его переживать нельзя.
            // Мигратор кэширует Task, поэтому тот же отказ повторно не
            // выстрелит и не превратится в цикл ретраев на каждый рестарт.
            logger.LogError(
                ex,
                "Schema migration host failed for {ContextName}",
                typeof(TContext).Name
            );

            Environment.Exit(1);
        }
    }
}
