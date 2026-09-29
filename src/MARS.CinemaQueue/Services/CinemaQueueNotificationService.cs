using MARS.CinemaQueue.Entities;
using MARS.CinemaQueue.Interfaces;
using Microsoft.Extensions.Hosting;

namespace MARS.CinemaQueue.Services;

public interface ICinemaQueueNotificationService
{
    Task CheckAndNotifyUnwatchedNextItemsAsync(CancellationToken cancellationToken = default);
}

public class CinemaQueueNotificationService(
    ICinemaQueueService cinemaQueueService,
    ILogger<CinemaQueueNotificationService> logger
) : BackgroundService, ICinemaQueueNotificationService
{
    private static readonly TimeSpan NotificationInterval = TimeSpan.FromDays(3);
    private DateTime _lastNotificationTime = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Cinema Queue Notification Service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndNotifyUnwatchedNextItemsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Cinema Queue Notification Service");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        logger.LogInformation("Cinema Queue Notification Service stopped");
    }

    public async Task CheckAndNotifyUnwatchedNextItemsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var now = DateTime.Now;

            if (now - _lastNotificationTime >= NotificationInterval)
            {
                logger.LogInformation("Checking for unwatched next items...");

                var allItems = await cinemaQueueService.GetAllMediaItemsAsync(cancellationToken);
                var nextItems = allItems
                    .Where(item => item is { IsNext: true, Status: MediaStatus.Pending })
                    .OrderBy(item => item.CreatedAt)
                    .ToList();

                if (nextItems.Count > 0)
                {
                    var unwatchedItems = nextItems
                        .Where(item => now - item.CreatedAt > NotificationInterval)
                        .ToList();

                    if (unwatchedItems.Count > 0)
                    {
                        LogNotification(unwatchedItems);
                        _lastNotificationTime = now;

                        logger.LogInformation(
                            "Processed notification for {Count} unwatched next items",
                            unwatchedItems.Count
                        );
                    }
                    else
                    {
                        logger.LogInformation("No unwatched next items older than 3 days found");
                    }
                }
                else
                {
                    logger.LogInformation("No unwatched next items found");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking unwatched next items");
        }
    }

    private void LogNotification(List<CinemaMediaItemDto> unwatchedItems)
    {
        if (unwatchedItems is { Count: > 0 })
        {
            var message = BuildNotificationMessage(unwatchedItems);
            logger.LogWarning("Cinema Queue Notification: {Message}", message);
        }
    }

    private static string BuildNotificationMessage(List<CinemaMediaItemDto> unwatchedItems)
    {
        var result = string.Empty;

        if (unwatchedItems is { Count: > 0 })
        {
            if (unwatchedItems.Count == 1)
            {
                var item = unwatchedItems.First();
                result =
                    $"Напоминание: фильм '{item.Title}' помечен как следующий для просмотра уже более 3 дней!";
            }
            else
            {
                var titles = unwatchedItems
                    .Take(3)
                    .Select(item => $"'{item.Title}'")
                    .ToList();

                result =
                    $"Напоминание: {unwatchedItems.Count} фильм(ов) помечены как следующие для просмотра уже более 3 дней: "
                    + string.Join(", ", titles);

                if (unwatchedItems.Count > 3)
                {
                    result += $" и еще {unwatchedItems.Count - 3}...";
                }
            }
        }

        return result;
    }
}
