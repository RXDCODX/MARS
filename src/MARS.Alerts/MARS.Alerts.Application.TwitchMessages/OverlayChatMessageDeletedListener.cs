using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchMessages;

/// <summary>
/// Ретранслирует удаление сообщения чата Twitch в оверлейный хаб.
/// </summary>
public sealed class OverlayChatMessageDeletedListener(
    ILogger<OverlayChatMessageDeletedListener> logger,
    EventBroadcaster<TelegramusEvent> broadcaster
) : MarsEventListener<ChatMessageDeletedEvent>
{
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

    public override async Task Handle(ChatMessageDeletedEvent message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogDebug("Chat message {MessageId} deleted", message.MessageId);

        await _broadcaster.BroadcastAsync(
            new TelegramusEvent
            {
                DeleteMessage = new DeleteMessageEvent { Id = message.MessageId },
            }
        );
    }
}
