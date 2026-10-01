using MARS.Shared.Clients;
using MARS.Shared.Messaging;
using MARS.Shared.Models;
using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Logging;

namespace MARS.TwitchCore.Services.HelloVideos;

/// <summary>
/// Реализация <see cref="IHelloVideoNotifier"/> через общий поток reward-событий.
/// У MARS.Alerts нет отдельного контракта для hello-video: он разбирает
/// <c>twitch.reward.redeemed</c> в <c>TwitchMediaAlerts</c> и отправляет
/// <c>Alert(MediaDto)</c>. Поэтому нотификатор публикует
/// <see cref="RewardRedeemedEvent"/> с уже подставленными
/// <see cref="RewardRedeemedEvent.User"/> и <see cref="RewardRedeemedEvent.Media"/>,
/// а <c>MediaInfo</c> получает у MARS.MediaStorage — владельца медиа.
/// </summary>
public class HelloVideoNotifier(
    IMarsEventBus eventBus,
    IMediaStorageClient mediaStorageClient,
    ILogger<HelloVideoNotifier> logger
) : IHelloVideoNotifier
{
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

        var rewardEvent = new RewardRedeemedEvent
        {
            RewardId = Guid.Empty.ToString(),
            RewardTitle = "hello-video",
            Cost = 0,
            UserName = alert.DisplayName,
            UserInput = alert.Message,
            User = new TwitchUser
            {
                TwitchId = string.Empty,
                UserLogin = alert.DisplayName,
                DisplayName = alert.DisplayName,
                ChatColor = alert.ChatColor,
            },
            Media = media,
        };

        await eventBus.PublishAsync(RabbitMqConfig.RewardRedeemed, rewardEvent);
    }
}
