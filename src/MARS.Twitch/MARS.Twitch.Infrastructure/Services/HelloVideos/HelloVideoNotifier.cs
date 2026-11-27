using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Twitch.Infrastructure.Services.HelloVideos;

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
    IMessageBus bus,
    IMarsGrpcClient grpcClient,
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

        MediaInfo? media;
        try
        {
            media = await grpcClient.MediaStorage.GetMediaInfoAsync(alert.MediaInfoId);
        }
        catch (RpcException)
        {
            media = null;
        }

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
            User = new MARS.Shared.Kernel.TwitchUser
            {
                TwitchId = string.Empty,
                UserLogin = alert.DisplayName,
                DisplayName = alert.DisplayName,
                ChatColor = alert.ChatColor,
            },
            Media = media,
        };

        await bus.PublishAsync(rewardEvent);
    }
}
