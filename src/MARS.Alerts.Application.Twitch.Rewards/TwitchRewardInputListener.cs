using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Wolverine listener сообщений, отправленных через награды с текстовым вводом.
/// Отдельно передаёт их в reward-input hook базовых обработчиков.
/// </summary>
public sealed class TwitchRewardInputListener(
    RewardEventManager eventManager,
    ILogger<TwitchRewardInputListener> logger
) : MarsEventListener<RewardInputChatMessageEvent>
{
    public override Task Handle(RewardInputChatMessageEvent message, CancellationToken ct)
    {
        if (message?.Message is null || string.IsNullOrWhiteSpace(message.Message.UserId))
        {
            logger.LogWarning("Twitch reward-input message has no deserializable user, skipped");
            return Task.CompletedTask;
        }

        return eventManager.HandleTwitchRewardInputAsync(message, ct);
    }
}
