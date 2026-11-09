using MARS.Platform.Infrastructure.Observability.Telemetry;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Единая точка маршрутизации событий Twitch для наград.
/// Получает редемпшены, обычные сообщения чата и reward-input, затем
/// вызывает нужный обработчик или его hook из <see cref="RewardHandlerBase"/>.
/// </summary>
public sealed class RewardEventManager
{
    private readonly IReadOnlyDictionary<string, IRewardAlertHandler> _rewardHandlers;
    private readonly IReadOnlyList<RewardHandlerBase> _eventHandlers;

    public RewardEventManager(
        IEnumerable<IRewardAlertHandler> handlers,
        ILogger<RewardEventManager> logger
    )
    {
        var registered = new Dictionary<string, IRewardAlertHandler>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        var allHandlers = handlers.ToArray();

        foreach (var handler in allHandlers)
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

        _rewardHandlers = registered;
        _eventHandlers = allHandlers.OfType<RewardHandlerBase>().ToArray();
    }

    /// <summary>Маршрутизирует погашение по нормализованному названию награды.</summary>
    public Task HandleRewardRedeemedAsync(RewardRedeemedEvent message, CancellationToken ct)
    {
        var rewardKey = RewardRouting.KeyFor(message.RewardTitle);

        if (!_rewardHandlers.TryGetValue(rewardKey, out var handler))
        {
            // Неизвестная награда — не ошибка: метрика показывает события без обработчика.
            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("reward_key", rewardKey)
            );
            return Task.CompletedTask;
        }

        return handler.HandleAsync(message, ct);
    }

    /// <summary>Передаёт обычное сообщение каждому обработчику-наследнику.</summary>
    public Task HandleTwitchChatMessageAsync(ChatEventMessageEvent message, CancellationToken ct) =>
        DispatchAsync(handler => handler.DispatchTwitchChatMessageAsync(message, ct));

    /// <summary>Передаёт reward-input только в отдельный hook, не смешивая его с обычным чатом.</summary>
    public Task HandleTwitchRewardInputAsync(RewardInputChatMessageEvent message, CancellationToken ct)
    {
        if (message?.Message is null)
        {
            return Task.CompletedTask;
        }

        return DispatchAsync(handler => handler.DispatchTwitchRewardInputAsync(message.Message, ct));
    }

    private async Task DispatchAsync(Func<RewardHandlerBase, Task> dispatch)
    {
        foreach (var handler in _eventHandlers)
        {
            await dispatch(handler);
        }
    }
}
