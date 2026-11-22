using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards.Listeners;

/// <summary>
/// Потребитель Wolverine-события RandomMemRequested (публикуется MARS.Telegram):
/// показывает случайный мем из Telegram-канала на оверлее.
/// </summary>
public sealed class RandomMemRequestedListener(
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ILogger<RandomMemRequestedListener> logger
) : MarsEventListener<RandomMemRequested>
{
    public override async Task Handle(RandomMemRequested message, CancellationToken ct)
    {
        if (message is null || message.Media is null)
        {
            return;
        }

        await hubContext.Clients.All.RandomMem(new AlertEvent { Media = message.Media });
        logger.LogDebug("RandomMem из Telegram доставлен в оверлей");
    }
}
