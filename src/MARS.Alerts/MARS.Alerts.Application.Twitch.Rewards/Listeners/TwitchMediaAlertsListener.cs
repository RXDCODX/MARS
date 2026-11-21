using MARS.Alerts.Application.Twitch.Rewards.Extensions;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Потребитель <see cref="RewardRedeemedEvent">событий редемпшна наград</see>:
/// превращает награду с медиа-контентом в gRPC-событие для источника OBS.
/// Реагирует только на события с заполненным <see cref="RewardRedeemedEvent.Media"/>.
/// </summary>
public sealed class TwitchMediaAlertsListener(
    ILogger<TwitchMediaAlertsListener> logger,
    EventBroadcaster<TelegramusEvent> broadcaster
) : MarsEventListener<RewardRedeemedEvent>
{
    public bool IsServiceActive { get; set; } = true;
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

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

        await _broadcaster.BroadcastAsync(
            new TelegramusEvent
            {
                Alert = new AlertEvent
                {
                    Media = MediaPayloadMapper.ToPayload(new MediaDto { MediaInfo = mediaClone }),
                },
            }
        );
    }
}
