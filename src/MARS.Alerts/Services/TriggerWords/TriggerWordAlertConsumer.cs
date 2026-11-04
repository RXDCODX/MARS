using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.TriggerWords;

/// <summary>
/// Потребитель сообщений чата: на каждом сообщении проверяет ключевые слова
/// алертов и показывает один случайный совпавший.
/// </summary>
/// <remarks>
/// Отдельная очередь <c>mars.alerts.trigger-words</c> с биндингом на сообщения чата
/// и на сообщения через награду (в монолите ключевое слово проверялось для обоих).
/// <c>ChatUserConsumer</c> читает свой поток (<c>chat.received</c>) из отдельной
/// очереди <c>mars.alerts.chat-users</c>: подбор алертов идёт из копии, чтобы
/// изменение правил не ломало отправку ответов в чат.
/// </remarks>
public class TriggerWordAlertConsumer(
    IOptions<RabbitMqOptions> options,
    TriggerWordAlertDispatcher dispatcher,
    ILogger<TriggerWordAlertConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        RabbitMqConfig.Queues.AlertsTriggerWords,
        [
            RabbitMqConfig.RoutingKeys.ChatReceived,
            RabbitMqConfig.RoutingKeys.ChatRewardInputReceived,
        ],
        logger
    )
{
    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var chatMessage = Deserialize<ChatMessageEvent>(json);

        if (chatMessage is null || string.IsNullOrWhiteSpace(chatMessage.Message))
        {
            return;
        }

        await dispatcher.DispatchAsync(chatMessage, ct);
    }
}
