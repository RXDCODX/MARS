using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Services.Twitch.Messages;

/// <summary>Wolverine listener уведомлений об удалении Twitch-сообщений.</summary>
public class TwitchMessageDeletedListener(ILogger<TwitchMessageDeletedListener> logger)
    : MarsEventListener<TwitchMessageDeletedEvent>
{
    public override Task Handle(
        TwitchMessageDeletedEvent message,
        CancellationToken cancellationToken
    ) => OnMessageDeletedAsync(message, cancellationToken);

    /// <summary>Точка расширения для последующей обработки удаления сообщения.</summary>
    protected virtual Task OnMessageDeletedAsync(
        TwitchMessageDeletedEvent message,
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
