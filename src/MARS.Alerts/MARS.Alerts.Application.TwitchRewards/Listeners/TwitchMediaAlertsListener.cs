using MARS.Alerts.Application.Extensions;
using MARS.Alerts.Application.TwitchRewards.Extensions;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Application.TwitchRewards.Listeners;

/// <summary>
/// Потребитель <see cref="RewardRedeemedEvent">событий редемпшна наград</see>:
/// превращает награду с медиа-контентом в gRPC-событие для источника OBS.
/// Реагирует только на события с заполненным <see cref="RewardRedeemedEvent.Media"/>.
/// </summary>
public sealed class TwitchMediaAlertsListener(
    ILogger<TwitchMediaAlertsListener> logger,
    IHubContext<AlertsHub, IAlertsHub> hubContext
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

        await hubContext.Clients.All.Alert(new AlertEvent { Media = mediaClone });
    }
}
