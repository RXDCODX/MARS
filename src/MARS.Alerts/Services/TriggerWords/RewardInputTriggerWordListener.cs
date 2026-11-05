using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.TriggerWords;

/// <summary>
/// Ключевые слова в сообщениях, отправленных через награду.
/// </summary>
/// <remarks>
/// Отдельная очередь от <see cref="TriggerWordAlertListener"/>: в монолите
/// ключевое слово проверялось и для обычных сообщений, и для наград, но смешивать
/// их в одной очереди нельзя — изменение правил для наград сломало бы обычный чат.
/// </remarks>
public sealed class RewardInputTriggerWordListener(TriggerWordAlertDispatcher dispatcher)
{
    public Task Handle(RewardInputMessageEvent message, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(message.Message)
            ? Task.CompletedTask
            : dispatcher.DispatchAsync(message, cancellationToken);
}
