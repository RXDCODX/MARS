using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchMessages;

public sealed class OverlayChatMessageDeletedListener(
    ILogger<OverlayChatMessageDeletedListener> logger,
    IHubContext<AlertsHub, IAlertsHub> hubContext
) : MarsEventListener<ChatMessageDeletedEvent>
{
    private readonly IHubContext<AlertsHub, IAlertsHub> _hubContext = hubContext;

    public override async Task Handle(ChatMessageDeletedEvent message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogDebug("Chat message {MessageId} deleted", message.MessageId);

        await _hubContext.Clients.All.DeleteMessage(
            new DeleteMessageEvent { Id = message.MessageId }
        );
    }
}