using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Единственный потребитель reward-событий в MARS.Alerts.
/// Читает очередь <c>alerts.rewards</c> с биндингом на
/// <see cref="RabbitMqConfig.RewardRedeemed"/> и диспетчеризует сообщения
/// зарегистрированным <see cref="IRewardAlertHandler"/> по внутреннему
/// <see cref="IRewardAlertHandler.RewardHandlerKey"/> (нормализованному
/// названию награды из события), а не по RabbitMQ routing key.
/// Раньше каждый из ~30 хендлеров был отдельной BackgroundService со своим
/// AMQP-соединением, каналом и собственной копией топологии exchange.
/// </summary>
public class RewardAlertConsumer(
    IOptions<RabbitMqOptions> options,
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<RewardAlertConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.RewardRedeemed],
        logger
    )
{
    public const string QueueName = "alerts.rewards";

    private readonly IReadOnlyDictionary<string, IRewardAlertHandler> _handlers = BuildHandlerMap(
        handlers,
        logger
    );

    private static IReadOnlyDictionary<string, IRewardAlertHandler> BuildHandlerMap(
        IEnumerable<IRewardAlertHandler> handlers,
        ILogger logger
    )
    {
        var registered = new Dictionary<string, IRewardAlertHandler>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var handler in handlers)
        {
            if (!registered.TryAdd(handler.RewardHandlerKey, handler))
            {
                duplicates.Add(handler.RewardHandlerKey);
            }
        }

        if (duplicates.Count > 0)
        {
            logger.LogWarning(
                "Duplicate reward alert handlers registered for keys: {Keys}. First registration wins.",
                string.Join(", ", duplicates)
            );
        }

        return registered;
    }

    protected override Task HandleMessageAsync(string routingKey, string json, CancellationToken ct)
    {
        var rewardEvent = Deserialize<RewardRedeemedEvent>(json);

        if (rewardEvent is null)
        {
            // Не удалось десериализовать событие — не ошибка, просто пропускаем.
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", routingKey)
            );
            return Task.CompletedTask;
        }

        var handlerKey = RabbitMqConfig.NormalizeRewardTitle(rewardEvent.RewardTitle);

        if (string.IsNullOrEmpty(handlerKey) || !_handlers.TryGetValue(handlerKey, out var handler))
        {
            // Нет обработчика для этой награды — не ошибка, не все награды что-то показывают.
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", routingKey)
            );
            return Task.CompletedTask;
        }

        return handler.HandleAsync(rewardEvent, ct);
    }
}
