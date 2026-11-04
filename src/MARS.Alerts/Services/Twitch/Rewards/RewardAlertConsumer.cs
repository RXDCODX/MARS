using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Единственный потребитель reward-событий в MARS.Alerts.
/// Читает очередь <c>alerts.rewards</c> с биндингом на
/// <see cref="RabbitMqConfig.RewardRedeemed"/> и на legacy routing key'ы
/// <see cref="RabbitMqConfig.LegacyRewardKeys"/> (для обратной совместимости
/// на период перехода: старые publisher'ы могут ещё слать
/// <c>twitch.reward.<name></c>). Диспетчеризует сообщения зарегистрированным
/// <see cref="IRewardAlertHandler"/> — ключ выводится из имени класса
/// (<c>ConfettiHandler</c> → <c>confetti</c>), а не из ручного свойства.
/// Раньше каждый из ~30 хендлеров был отдельной BackgroundService со своим
/// AMQP-соединением, каналом и собственной копией топологии exchange.
/// </summary>
public class RewardAlertConsumer(
    IOptions<RabbitMqOptions> options,
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<RewardAlertConsumer> logger,
    Func<IRewardAlertHandler, string>? keySelector = null
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.RewardRedeemed, .. RabbitMqConfig.LegacyRewardKeys],
        logger
    )
{
    public const string QueueName = "alerts.rewards";

    private readonly IReadOnlyDictionary<string, IRewardAlertHandler> _handlers = BuildHandlerMap(
        handlers,
        logger,
        keySelector ?? DefaultKeySelector
    );

    private static IReadOnlyDictionary<string, IRewardAlertHandler> BuildHandlerMap(
        IEnumerable<IRewardAlertHandler> handlers,
        ILogger logger,
        Func<IRewardAlertHandler, string> keySelector
    )
    {
        var registered = new Dictionary<string, IRewardAlertHandler>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var handler in handlers)
        {
            var key = keySelector(handler);

            if (!registered.TryAdd(key, handler))
            {
                duplicates.Add(key);
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

    private static string DefaultKeySelector(IRewardAlertHandler handler)
    {
        // Ключ выводится из имени класса: ConfettiHandler → confetti.
        var key = handler.GetType().Name;
        if (key.EndsWith("Handler", StringComparison.Ordinal))
        {
            key = key[..^"Handler".Length];
        }
        return key.ToLowerInvariant();
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
            // Внимание: диспетч по названию награды rename-sensitive — переименование
            // награды в Twitch меняет lookup key. Для production-устойчивости нужен
            // mapping RewardId → handler (отдельное решение).
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", routingKey)
            );
            return Task.CompletedTask;
        }

        return handler.HandleAsync(rewardEvent, ct);
    }
}
