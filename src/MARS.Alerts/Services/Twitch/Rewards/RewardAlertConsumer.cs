using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Platform.Observability.Telemetry.Infrastructure;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Единственный потребитель reward-событий в MARS.Alerts.
/// Читает очередь <c>alerts.rewards</c> и раздаёт сообщения зарегистрированным
/// <see cref="IRewardAlertHandler"/> по ключу награды.
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

        foreach (var handler in handlers)
        {
            if (!registered.TryAdd(handler.RewardKey, handler))
            {
                duplicates.Add(handler.RewardKey);
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
            throw new InvalidOperationException(
                $"Reward event on '{routingKey}' could not be deserialized into RewardRedeemedEvent"
            );
        }

        var rewardKey = RewardRouting.KeyFor(rewardEvent.RewardTitle);

        if (!_handlers.TryGetValue(rewardKey, out var handler))
        {
            // Неизвестная награда — не ошибка: базовый класс уже подтвердил доставку.
            // Счётчик показывает награды, для которых в MARS.Alerts нет хендлера.
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("reward_key", rewardKey)
            );
            return Task.CompletedTask;
        }

        return handler.HandleAsync(rewardEvent, ct);
    }
}
