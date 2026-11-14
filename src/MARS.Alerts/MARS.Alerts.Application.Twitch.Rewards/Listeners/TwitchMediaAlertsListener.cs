using MARS.Alerts.Application.Twitch.Rewards.Extensions;
using MARS.Platform.Infrastructure.Grpc.Notifications;

using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Потребитель <see cref="RewardRedeemedEvent">событий редемпшна наград</see>:
/// превращает награду с медиа-контентом в gRPC-событие для источника OBS.
/// Реагирует только на события с заполненным <see cref="RewardRedeemedEvent.Media"/>.
/// </summary>
public sealed class TwitchMediaAlertsListener(
    ITelegramusNotifier notifier,
    ILogger<TwitchMediaAlertsListener> logger
) : MarsEventListener<RewardRedeemedEvent>
{
    public bool IsServiceActive { get; set; } = true;

    public override async Task Handle(RewardRedeemedEvent message, CancellationToken ct)
    {
        if (message is null)
        {
            logger.LogWarning("Reward event could not be deserialized, skipped");
            return;
        }

        if (IsServiceActive && message.Media is not null)
        {
            await SendAlertAsync(
                message.Media,
                message.User,
                message.UserInput ?? string.Empty,
                ct
            );
        }
    }

    private async Task SendAlertAsync(
        MediaInfo media,
        TwitchUser? user,
        string message,
        CancellationToken ct
    )
    {
        var mediaClone = media.CloneTo();

        if (user is not null)
        {
            mediaClone.FixAlertText(user, message);
            mediaClone.FixAlertColor(user);
        }

        ct.ThrowIfCancellationRequested();

        await notifier.Alert(new MediaDto { MediaInfo = mediaClone });
    }
}
