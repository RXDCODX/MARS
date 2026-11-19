using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Принимает обычные сообщения Twitch-чата и передаёт их общему менеджеру
/// наград. Менеджер активирует OnTwitchMessage у каждого подписанного обработчика.
/// </summary>
public sealed class ChatUserListener(
    RewardEventManager eventManager,
    ILogger<ChatUserListener> logger
) : MarsEventListener<ChatEventMessageEvent>
{
    public override Task Handle(ChatEventMessageEvent message, CancellationToken ct)
    {
        if (message is null || string.IsNullOrWhiteSpace(message.UserId))
        {
            logger.LogWarning("Chat message has no deserializable user, skipped");
            return Task.CompletedTask;
        }

        return eventManager.HandleTwitchChatMessageAsync(message, ct);
    }
}
