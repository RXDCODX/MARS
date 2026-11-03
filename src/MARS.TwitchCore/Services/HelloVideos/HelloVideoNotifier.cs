using System.Text.Json;
using MARS.Shared.Clients;
using MARS.Shared.Grpc.Notifications;
using MARS.Shared.Models;
using MARS.Shared.Models.Rewards;
using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Logging;

namespace MARS.TwitchCore.Services.HelloVideos;

/// <summary>
/// Публикует приветственное видео через тот же универсальный JSON-канал
/// TelegramusHub, что и Twitch EventSub и активации Channel Points.
/// Hello-video не является Twitch redemption, поэтому не подделывает
/// Twitch metadata/payload и не использует отдельный RabbitMQ routing key.
/// </summary>
public class HelloVideoNotifier(
    ITelegramusNotifier notifier,
    IMediaStorageClient mediaStorageClient,
    ILogger<HelloVideoNotifier> logger
) : IHelloVideoNotifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendAlertAsync(HelloVideoAlertDto alert)
    {
        if (alert is null || alert.MediaInfoId == Guid.Empty)
        {
            logger.LogWarning(
                "Hello-video alert without media id skipped for {DisplayName}",
                alert?.DisplayName
            );

            return;
        }

        var media = await mediaStorageClient.GetMediaInfoAsync(alert.MediaInfoId);

        if (media is null)
        {
            logger.LogWarning(
                "Media {MediaInfoId} for hello-video alert of {DisplayName} not found in MARS.MediaStorage",
                alert.MediaInfoId,
                alert.DisplayName
            );

            return;
        }

        var eventJson = JsonSerializer.Serialize(
            new
            {
                mars = new
                {
                    reward = new
                    {
                        type = "mars.reward.activated",
                        eventId = $"hello-video:{Guid.NewGuid():N}",
                        payload = new
                        {
                            rewardId = "mars.hello-video",
                            rewardTitle = "Приветственное видео",
                            alertKey = RewardAlertKeys.HelloVideo,
                            cost = 0,
                            userInput = alert.Message,
                            redeemedAt = DateTimeOffset.UtcNow,
                            user = new TwitchUser
                            {
                                TwitchId = string.Empty,
                                UserLogin = alert.DisplayName,
                                DisplayName = alert.DisplayName,
                                ChatColor = alert.ChatColor,
                            },
                            media = new[] { media },
                            effectData = (object?)null,
                        },
                    },
                },
            },
            JsonOptions
        );

        await notifier.TwitchEventSub(eventJson);
    }
}
