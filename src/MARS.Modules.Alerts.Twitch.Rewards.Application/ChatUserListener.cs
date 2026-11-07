using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Потребитель <see cref="ChatEventMessageEvent">обычных сообщений чата</see>:
/// единственный источник данных для <see cref="IChatUserTrackingHandler"/>.
/// </summary>
public sealed class ChatUserListener(
    IEnumerable<IChatUserTrackingHandler> handlers,
    ILogger<ChatUserListener> logger
) : MarsEventListener<ChatEventMessageEvent>
{
    private readonly IReadOnlyList<IChatUserTrackingHandler> _handlers = [.. handlers];

    public override Task Handle(ChatEventMessageEvent message, CancellationToken ct)
    {
        if (_handlers.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (message is null || string.IsNullOrWhiteSpace(message.UserId))
        {
            logger.LogWarning("Chat message has no deserializable user, skipped");
            return Task.CompletedTask;
        }

        foreach (var handler in _handlers)
        {
            handler.TrackChatUser(message);
        }

        return Task.CompletedTask;
    }
}
