using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;

namespace MARS.Alerts.Application.TwitchRewards.Handlers;

/// <summary>
/// Общая основа Twitch-наград. Конкретная награда объявляет метаданные,
/// подписывается на нужные события через <see cref="SubscribeOnStart"/> и
/// обрабатывает редемпшен в собственном методе.
/// </summary>
public abstract class RewardHandlerBase : IRewardAlertHandler
{
    public virtual int RewardCost => Cost;
    public virtual string AlertDisplayName { get; set; } = string.Empty;
    public virtual string AlertDescription { get; set; } = string.Empty;
    public virtual Color Color { get; set; } = Color.White;
    public virtual int Cost { get; init; }
    public virtual Func<bool> IsRewardEnabled { get; set; } = () => true;
    public virtual Guid? TwitchRewardId { get; private protected set; }

    public virtual bool IsRewardActivityWasConfermed { get; set; }
    public virtual bool IsRewardActive => IsRewardEnabled();

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Прямой вызов обработчика (используется тестами и совместимымиconsumer'ами).
    /// </summary>
    public Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rewardEvent);

        if (rewardEvent.Cost != Cost || !IsRewardEnabled())
            return Task.CompletedTask;

        return OnRewardRedeemedAsync(rewardEvent, ct);
    }

    public virtual IRewardActivationPolicy Activation => AlwaysActiveRewardPolicy.Instance;

    /// <summary>Тип команды, обслуживаемой обработчиком (вместо строкового ключа).</summary>
    public virtual Type? CommandPayloadType => null;

    public virtual Task<string> InvokeFromCommandAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    ) => Task.FromResult("Обработчик не поддерживает вызов из команд.");

    /// <summary>
    /// Точка входа для редемпшена, которую могут переопределять тестовые/stub-
    /// обработчики. Боевые хендлеры используют свой приватный метод и передают
    /// его в <see cref="ValidatedRedeem"/>.
    /// </summary>
    protected virtual Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    ) => Task.CompletedTask;

    /// <summary>
    /// Валидирует редемпшен и вызывает обработчик только при успехе.
    /// Follower-проверка включается только для наград, где она была изначально.
    /// </summary>
    protected Func<RewardRedeemedEvent, CancellationToken, Task> ValidatedRedeem(
        ITwitchEventValidationService validator,
        Func<RewardRedeemedEvent, CancellationToken, Task> handler,
        int cost,
        Func<bool> isEnabled,
        bool requireFollower = false
    ) =>
        async (rewardEvent, ct) =>
        {
            var chain = validator
                .ForRedemption(rewardEvent)
                .RequireBroadcasterUserId()
                .RequireRewardEnabled(isEnabled)
                .RequireCost(cost);

            if (requireFollower)
                chain = chain.RequireFollower();

            var vr = await chain.ValidateWithResponseAsync(
                rewardEvent.UserName ?? rewardEvent.User?.DisplayName ?? string.Empty
            );

            if (vr.IsInvalid)
                return;

            await handler(rewardEvent, ct);
        };

    /// <summary>
    /// Подписывает обработчика на старте и отписывает на остановке.
    /// </summary>
    protected void SubscribeOnStart(
        IHostApplicationLifetime lifetime,
        IRewardEventManager events,
        Func<RewardRedeemedEvent, CancellationToken, Task>? onRedeem = null,
        Func<ChatEventMessageEvent, CancellationToken, Task>? onChat = null,
        Func<EventSubRawEvent, CancellationToken, Task>? onEventSub = null,
        Func<RewardInputChatMessageEvent, CancellationToken, Task>? onRewardInput = null
    )
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            if (onRedeem is not null)
                events.OnTwitchRewardRedeem += onRedeem;
            if (onChat is not null)
                events.OnTwitchMessage += onChat;
            if (onEventSub is not null)
                events.OnTwitchEvent += onEventSub;
            if (onRewardInput is not null)
                events.OnTwitchRewardInput += onRewardInput;
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            if (onRedeem is not null)
                events.OnTwitchRewardRedeem -= onRedeem;
            if (onChat is not null)
                events.OnTwitchMessage -= onChat;
            if (onEventSub is not null)
                events.OnTwitchEvent -= onEventSub;
            if (onRewardInput is not null)
                events.OnTwitchRewardInput -= onRewardInput;
        });
    }
}
