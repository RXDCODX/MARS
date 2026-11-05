using System.Text;
using MARS.Shared.Messaging;
using MARS.Shared.Telemetry;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Раздача погашений наград зарегистрированным <see cref="IRewardAlertHandler"/>.
/// </summary>
/// <remarks>
/// <para>
/// Ключ выводится из имени класса (<c>ConfettiHandler</c> → <c>confetti</c>), а не
/// из ручного свойства: список хендлеров берётся рефлексией, и ручной ключ пришлось
/// бы поддерживать в двух местах. Название награды нормализуется тем же правилом
/// (нижний регистр, только буквы и цифры), поэтому «Аяка!!!» и «аяка» — один ключ.
/// </para>
/// <para>
/// Награда без обработчика — не ошибка: у неё может не быть визуального эффекта, и
/// сообщение обязано подтвердиться, а не уйти в повтор и dead-letter очередь.
/// Отсутствие ключа видно в журнале и в счётчике.
/// </para>
/// </remarks>
public sealed class RewardAlertDispatcher(
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<RewardAlertDispatcher> logger,
    Func<IRewardAlertHandler, string>? keySelector = null
)
{
    private readonly IReadOnlyDictionary<string, IRewardAlertHandler> _handlers = BuildHandlerMap(
        handlers,
        logger,
        keySelector ?? DefaultKeySelector
    );

    public async Task DispatchAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken cancellationToken
    )
    {
        var handlerKey = NormalizeRewardTitle(rewardEvent.RewardTitle);

        if (!string.IsNullOrEmpty(handlerKey) && _handlers.TryGetValue(handlerKey, out var handler))
        {
            await handler.HandleAsync(rewardEvent, cancellationToken);
        }
        else
        {
            logger.LogDebug(
                "Для награды {RewardTitle} (ключ {HandlerKey}) нет обработчика — событие пропущено",
                rewardEvent.RewardTitle,
                handlerKey
            );

            MarsMetrics.RabbitMqUnhandledMessages.Add(
                1,
                new KeyValuePair<string, object?>("routing_key", handlerKey)
            );
        }
    }

    /// <summary>
    /// Нормализует название награды Twitch в идентификатор обработчика: регистр в
    /// нижний, все символы кроме букв и цифр отбрасываются.
    /// </summary>
    /// <remarks>
    /// Диспетчеризация по названию награды чувствительна к переименованию в
    /// Twitch: это известное ограничение, отдельное решение — mapping
    /// RewardId → handler.
    /// </remarks>
    private static string NormalizeRewardTitle(string rewardTitle)
    {
        var normalized = new StringBuilder(rewardTitle.Length);

        foreach (var symbol in rewardTitle)
        {
            if (char.IsLetterOrDigit(symbol))
            {
                normalized.Append(char.ToLowerInvariant(symbol));
            }
        }

        return normalized.ToString();
    }

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
        var key = handler.GetType().Name;

        if (key.EndsWith("Handler", StringComparison.Ordinal))
        {
            key = key[..^"Handler".Length];
        }

        return key.ToLowerInvariant();
    }
}
