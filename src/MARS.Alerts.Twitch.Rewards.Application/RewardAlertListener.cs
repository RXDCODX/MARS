using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.Platform.Observability.Telemetry.Infrastructure;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Единственный потребитель reward-событий в MARS.Alerts.
/// Разбирает каждый <see cref="RewardRedeemedEvent">событие редемпшна</see> и
/// передаёт его обработчику, ключ которого совпадает с нормализованным
/// названием награды.
/// </summary>
public sealed class RewardAlertListener(
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<RewardAlertListener> logger
) : MarsEventListener<RewardRedeemedEvent>
{
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

    public override Task Handle(RewardRedeemedEvent message, CancellationToken ct)
    {
        var rewardKey = RewardRouting.KeyFor(message.RewardTitle);

        if (!_handlers.TryGetValue(rewardKey, out var handler))
        {
            // Неизвестная награда — не ошибка. Счётчик показывает награды,
            // для которых в MARS.Alerts нет хендлера.
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("reward_key", rewardKey)
            );
            return Task.CompletedTask;
        }

        return handler.HandleAsync(message, ct);
    }
}
