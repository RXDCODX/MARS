using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Handlers;

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
    public abstract string RewardCode { get; }

    public virtual MARS.Shared.Contracts.Rewards.RewardDefinition Definition =>
        MARS.Shared.Contracts.Rewards.RewardCatalog.Get(RewardCode);

    public virtual IRewardActivationPolicy Activation => AlwaysActiveRewardPolicy.Instance;

    /// <summary>
    /// Ключ команды, которую обслуживает этот обработчик (совпадает с
    /// дискриминатором <c>$type</c> у <see cref="AlertCommandPayload"/>).
    /// null — обработчик не вызывается из команд.
    /// </summary>
    public virtual string? CommandKey => null;

    /// <summary>
    /// Вызов действия обработчика из команды. Конкретный обработчик переопределяет
    /// метод и читает свой наследник <see cref="AlertCommandPayload"/>.
    /// </summary>
    public virtual Task<string> InvokeFromCommandAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    ) => Task.FromResult("Обработчик не поддерживает вызов из команд.");

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
