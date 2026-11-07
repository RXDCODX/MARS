using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Modules.Alerts.Twitch.Messages.Application;

/// <summary>
/// Wolverine listener сообщений, отправленных через channel point reward.
/// Получает отдельный тип контракта и собственную очередь.
/// </summary>
public class TwitchRewardInputMessageListener(ILogger<TwitchRewardInputMessageListener> logger)
    : MarsEventListener<RewardInputChatMessageEvent>
{
    public override Task Handle(
        RewardInputChatMessageEvent message,
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
