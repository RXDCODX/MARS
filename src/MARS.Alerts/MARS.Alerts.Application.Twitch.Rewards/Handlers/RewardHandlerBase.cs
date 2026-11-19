using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Общая основа Twitch-наград. Конкретная награда обрабатывает редемпшен
/// через <see cref="OnRewardRedeemedAsync"/> и подписывается только на те
/// входящие события Twitch, которые ей нужны.
/// </summary>
/// <remarks>
/// <para>
/// События вызываются диспетчером для каждого зарегистрированного наследника.
/// Подписчики одного обработчика выполняются последовательно и ожидаются:
/// ошибка одного подписчика не теряется и передаётся Wolverine для штатной
/// обработки сообщения.
/// </para>
/// <para>
/// Обычный чат, текстовый ввод channel-point и EventSub — разные типы событий.
/// Reward-input не попадает в обычный поток сообщений чата.
/// </para>
/// </remarks>
public abstract class RewardHandlerBase : IRewardAlertHandler
{
    public abstract string RewardKey { get; }

    /// <summary>Обычное сообщение Twitch-чата.</summary>
    public event Func<ChatEventMessageEvent, CancellationToken, Task>? OnTwitchMessage;

    /// <summary>Сообщение, отправленное через channel-point reward с текстовым вводом.</summary>
    public event Func<ChatEventMessageEvent, CancellationToken, Task>? OnTwitchRewardInput;

    /// <summary>
    /// Сырой EventSub-конверт, опубликованный MARS.TwitchCore.
    /// Конкретный обработчик может фильтровать его по EventName и SubscriptionType.
    /// </summary>
    public event Func<EventSubRawEvent, CancellationToken, Task>? OnTwitchEvent;

    /// <summary>Единая точка входа при погашении конкретной награды.</summary>
    public Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rewardEvent);
        return OnRewardRedeemedAsync(rewardEvent, ct);
    }

    /// <summary>Логика погашения, специфичная для конкретной награды.</summary>
    protected abstract Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    );

    internal Task DispatchTwitchChatMessageAsync(
        ChatEventMessageEvent message,
        CancellationToken ct
    ) => DispatchAsync(OnTwitchMessage, message, ct);

    internal Task DispatchTwitchRewardInputAsync(
        ChatEventMessageEvent message,
        CancellationToken ct
    ) => DispatchAsync(OnTwitchRewardInput, message, ct);

    internal Task DispatchTwitchEventAsync(EventSubRawEvent twitchEvent, CancellationToken ct) =>
        DispatchAsync(OnTwitchEvent, twitchEvent, ct);

    private static async Task DispatchAsync<TEvent>(
        Func<TEvent, CancellationToken, Task>? subscribers,
        TEvent message,
        CancellationToken ct
    )
    {
        if (subscribers is null)
        {
            return;
        }

        // Для multicast delegate прямой await subscribers(...) ожидает только
        // Task последнего подписчика. Поэтому каждый асинхронный обработчик
        // вызывается и ожидается отдельно.
        foreach (var subscriber in subscribers.GetInvocationList())
        {
            ct.ThrowIfCancellationRequested();
            await ((Func<TEvent, CancellationToken, Task>)subscriber)(message, ct);
        }
    }
}
