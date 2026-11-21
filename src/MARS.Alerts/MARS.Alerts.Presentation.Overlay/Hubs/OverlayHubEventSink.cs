using MARS.Alerts.Application.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Presentation.Overlay.Hubs;

public sealed class OverlayHubEventSink(IHubContext<AlertsHub, IAlertsHub> hubContext) : IOverlayEventSink
{
    public Task BroadcastAsync(TelegramusEvent notification, CancellationToken cancellationToken = default) =>
        OverlayEventDispatcher.DispatchAsync(hubContext, notification, cancellationToken);
}