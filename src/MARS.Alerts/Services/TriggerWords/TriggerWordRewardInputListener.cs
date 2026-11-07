using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Services.TriggerWords;

/// <summary>
/// Потребитель сообщений, отправленных через награду: проверяет ключевые слова
/// алертов на texte сообщения из <see cref="RewardInputChatMessageEvent"/>.
/// </summary>
public sealed class TriggerWordRewardInputListener(TriggerWordAlertDispatcher dispatcher)
    : MarsEventListener<RewardInputChatMessageEvent>
{
    public override async Task Handle(RewardInputChatMessageEvent message, CancellationToken ct)
    {
        if (message?.Message is null || string.IsNullOrWhiteSpace(message.Message.Message))
        {
            return;
        }

        await dispatcher.DispatchAsync(message.Message, ct);
    }
}
