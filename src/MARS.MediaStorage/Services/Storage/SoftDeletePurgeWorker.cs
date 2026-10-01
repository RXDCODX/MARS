using MARS.MediaStorage.Services.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.MediaStorage.Services.Storage;

/// <summary>
/// Периодически удаляет из корзины файлы, у которых истёк срок восстановления.
/// </summary>
/// <remarks>
/// Аудит Stage 3: мягкое удаление без автоматической очистки превращало корзину
/// в место, где файлы живут вечно. Служба не должна падать из-за одного
/// недоступного файла, поэтому исключения перехватываются.
/// </remarks>
public sealed class SoftDeletePurgeWorker(
    IServiceScopeFactory scopeFactory,
    MediaStorageOptions options,
    ILogger<SoftDeletePurgeWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.EnablePurgeWorker)
        {
            logger.LogInformation("Фоновая очистка корзины выключена");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, options.PurgeIntervalMinutes));

        logger.LogInformation(
            "Очистка корзины: каждые {Interval}, срок восстановления {Retention} дней",
            interval,
            options.TrashRetentionDays
        );

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await PurgeOnceAsync(stoppingToken);
        }
    }

    private async Task PurgeOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();

            var purged = await storage.PurgeExpiredAsync(cancellationToken);

            if (purged > 0)
            {
                logger.LogInformation("Безвозвратно удалено из корзины: {Count}", purged);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Очистка корзины не удалась, будет повторена позже");
        }
    }
}
