using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Единственный потребитель reward-событий в MARS.Alerts.
/// Читает очередь <c>alerts.rewards</c> и раздаёт сообщения зарегистрированным
/// <see cref="IRewardAlertHandler"/> по routing key'ам из
/// <see cref="RabbitMqConfig.RewardSpecificKeys"/>.
/// Раньше каждый из ~30 хендлеров был отдельной BackgroundService со своим AMQP-соединением,
/// каналом и собственной копией топологии exchange.
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
        RabbitMqConfig.RewardSpecificKeys,
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
        var unbound = new List<string>();

        foreach (var handler in handlers)
        {
            if (!registered.TryAdd(handler.RoutingKey, handler))
            {
                duplicates.Add(handler.RoutingKey);
            }

            if (!RabbitMqConfig.RewardSpecificKeys.Contains(handler.RoutingKey))
            {
                unbound.Add(handler.RoutingKey);
            }
        }

        if (duplicates.Count > 0)
        {
            logger.LogWarning(
                "Duplicate reward alert handlers registered for keys: {Keys}. First registration wins.",
                string.Join(", ", duplicates)
            );
        }

        if (unbound.Count > 0)
        {
            logger.LogWarning(
                "Reward alert handlers registered for keys that are not bound to {Queue}: {Keys}",
                RewardAlertConsumer.QueueName,
                string.Join(", ", unbound)
            );
        }

        return registered;
    }

    protected override Task HandleMessageAsync(string routingKey, string json, CancellationToken ct)
    {
        if (!_handlers.TryGetValue(routingKey, out var handler))
        {
            // Неизвестный routing key — не ошибка: базовый класс уже подтвердил доставку.
            // Такой счётчик показывает награды, для которых в MARS.Alerts нет хендлера.
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", routingKey)
            );
            return Task.CompletedTask;
        }

        var rewardEvent = Deserialize<RewardRedeemedEvent>(json);

        if (rewardEvent is null)
        {
            throw new InvalidOperationException(
                $"Reward event on '{routingKey}' could not be deserialized into RewardRedeemedEvent"
            );
        }

        return handler.HandleAsync(rewardEvent, ct);
    }
}
