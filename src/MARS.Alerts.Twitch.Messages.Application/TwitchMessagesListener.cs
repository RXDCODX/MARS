using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application;

/// <summary>
/// Wolverine listener обычных сообщений чата Twitch.
/// </summary>
public class TwitchMessagesListener(ILogger<TwitchMessagesListener> logger)
    : MarsEventListener<ChatEventMessageEvent>
{
    public override Task Handle(
        ChatEventMessageEvent message,
        CancellationToken cancellationToken
    ) => OnMessageReceivedAsync(message, cancellationToken);

    /// <summary>Точка расширения для последующей обработки обычного сообщения чата.</summary>
    protected virtual Task OnMessageReceivedAsync(
        ChatEventMessageEvent message,
        CancellationToken ct
    )
    {
        logger.LogDebug("Twitch chat message received from {UserName}", message.UserName);

        return Task.CompletedTask;
    }
}
