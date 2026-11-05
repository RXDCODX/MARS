using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.TriggerWords;

/// <summary>
/// Ключевые слова в обычных сообщениях чата: показывает один случайный совпавший
/// алерт.
/// </summary>
public sealed class TriggerWordAlertListener(TriggerWordAlertDispatcher dispatcher)
{
    public Task Handle(ChatMessageReceivedEvent message, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(message.Message)
            ? Task.CompletedTask
            : dispatcher.DispatchAsync(message, cancellationToken);
}
