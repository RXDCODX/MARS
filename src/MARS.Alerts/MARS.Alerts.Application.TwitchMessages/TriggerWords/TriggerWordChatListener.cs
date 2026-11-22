using MARS.Alerts.Application.TwitchMessages.TriggerWords;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchMessages.TriggerWords;

/// <summary>
/// Потребитель обычных сообщений чата: на каждом сообщении проверяет ключевые
/// слова алертов и показывает один случайный совпавший.
/// </summary>
public sealed class TriggerWordChatListener(TriggerWordAlertDispatcher dispatcher)
    : MarsEventListener<ChatEventMessageEvent>
{
    public override async Task Handle(ChatEventMessageEvent message, CancellationToken ct)
    {
        if (message is null || string.IsNullOrWhiteSpace(message.Message))
        {
            return;
        }

        await dispatcher.DispatchAsync(message, ct);
    }
}
