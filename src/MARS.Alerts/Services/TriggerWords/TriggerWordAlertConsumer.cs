using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.TriggerWords;

/// <summary>
/// Потребитель сообщений чата: на каждом сообщении проверяет ключевые слова
/// алертов и показывает один случайный совпавший.
/// </summary>
/// <remarks>
/// Отдельная очередь <c>alerts.triggerwords</c> с биндингом на сообщения чата
/// и на сообщения через награду (в монолите ключевое слово проверялось для обоих): <c>ChatUserConsumer</c> на той же очереди
/// <c>alerts.events</c> должен получать обычный поток сообщений, а подбор
/// алертов — из копии, чтобы изменение правил не ломало отправку ответов в чат.
/// </remarks>
public class TriggerWordAlertConsumer(
    IOptions<RabbitMqOptions> options,
    TriggerWordAlertDispatcher dispatcher,
    ILogger<TriggerWordAlertConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.MessageReceived, RabbitMqConfig.RewardInputMessage],
        logger
    )
{
    public const string QueueName = "alerts.triggerwords";

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
