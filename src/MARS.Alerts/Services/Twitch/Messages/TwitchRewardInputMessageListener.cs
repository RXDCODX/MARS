using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Services.Twitch.Messages;

/// <summary>
/// Wolverine listener сообщений, отправленных через channel point reward.
/// Получает отдельный тип контракта и собственную очередь.
/// </summary>
public class TwitchRewardInputMessageListener(ILogger<TwitchRewardInputMessageListener> logger)
    : MarsEventListener<TwitchRewardInputMessageEvent>
{
    public override Task Handle(
        TwitchRewardInputMessageEvent message,
        CancellationToken cancellationToken
    ) => OnRewardInputReceivedAsync(message.Message, cancellationToken);

    /// <summary>Точка расширения для последующей обработки текста reward-input.</summary>
    protected virtual Task OnRewardInputReceivedAsync(
        ChatEventMessageEvent message,
        CancellationToken ct
    )
    {
        logger.LogDebug(
            "Twitch reward-input message received from {UserName}, reward={RewardId}",
            message.UserName,
            message.CustomRewardId
        );

        return Task.CompletedTask;
    }
}
