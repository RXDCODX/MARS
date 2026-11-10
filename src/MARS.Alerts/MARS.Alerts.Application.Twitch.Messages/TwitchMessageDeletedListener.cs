using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Messages;

/// <summary>Wolverine listener уведомлений об удалении Twitch-сообщений.</summary>
public class TwitchMessageDeletedListener(ILogger<TwitchMessageDeletedListener> logger)
    : MarsEventListener<ChatMessageDeletedEvent>
{
    public override Task Handle(
        ChatMessageDeletedEvent message,
        CancellationToken cancellationToken
    ) => OnMessageDeletedAsync(message, cancellationToken);

    /// <summary>Точка расширения для последующей обработки удаления сообщения.</summary>
    protected virtual Task OnMessageDeletedAsync(
        ChatMessageDeletedEvent message,
        CancellationToken ct
    )
    {
        logger.LogDebug(
            "Twitch chat message {MessageId} deleted in {Channel}",
            message.MessageId,
            message.Channel
        );

        return Task.CompletedTask;
    }
}
