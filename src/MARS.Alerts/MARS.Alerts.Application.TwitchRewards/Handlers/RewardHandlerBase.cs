using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace MARS.Alerts.Application.TwitchRewards.Handlers;

/// <summary>
/// Общая основа Twitch-наград. Конкретная награда объявляет метаданные,
/// подписывается на нужные события через <see cref="SubscribeOnStart"/> и
/// обрабатывает редемпшен в собственном методе.
/// </summary>
public abstract class RewardHandlerBase : IRewardAlertHandler, IRewardDefinitionProvider
{
    public virtual int RewardCost => Cost;
    public virtual string AlertDisplayName { get; set; } = string.Empty;
    public virtual string AlertDescription { get; set; } = string.Empty;
    public virtual Color Color { get; set; } = Color.White;
    public virtual bool IsUserInputRequired { get; } = false;
    public virtual int Cost { get; init; }
    public virtual Guid? TwitchRewardId { get; private protected set; }

    /// <summary>
    /// Стабильный код обработчика. По умолчанию — имя класса.
    /// </summary>
    public virtual string RewardCode => GetType().Name;

    /// <summary>
    /// Presence-aware настройки Twitch. null означает «сохранить локальную настройку».
    /// </summary>
    public virtual bool? IsMaxPerStreamEnabled { get; } = null;
    public virtual int? MaxPerStream { get; } = null;
    public virtual bool? IsMaxPerUserPerStreamEnabled { get; } = null;
    public virtual int? MaxPerUserPerStream { get; } = null;
    public virtual bool? IsGlobalCooldownEnabled { get; } = null;
    public virtual int? GlobalCooldownSeconds { get; } = null;
    public virtual bool? ShouldRedemptionsSkipRequestQueue { get; } = null;

    public virtual RewardDefinitionSnapshot CreateDefinitionSnapshot(bool isActive) =>
        new(
            Cost,
            AlertDisplayName,
            AlertDescription,
            ColorTranslator.ToHtml(Color),
            IsUserInputRequired,
            isActive,
            RewardCode,
            IsMaxPerStreamEnabled,
            MaxPerStream,
            IsMaxPerUserPerStreamEnabled,
            MaxPerUserPerStream,
            IsGlobalCooldownEnabled,
            GlobalCooldownSeconds,
            ShouldRedemptionsSkipRequestQueue
        );

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private protected virtual CreateCustomRewardsRequest CreateCustomRewardsRequest =>
        new()
        {
            Title = AlertDisplayName,
            Prompt = AlertDescription,
            Cost = Cost,
            IsEnabled = true,
            IsUserInputRequired = IsUserInputRequired,
            IsMaxPerStreamEnabled = IsMaxPerStreamEnabled ?? false,
            MaxPerStream = MaxPerStream,
            IsMaxPerUserPerStreamEnabled = IsMaxPerUserPerStreamEnabled ?? false,
            MaxPerUserPerStream = MaxPerUserPerStream,
            IsGlobalCooldownEnabled = IsGlobalCooldownEnabled ?? false,
            GlobalCooldownSeconds = GlobalCooldownSeconds,
            ShouldRedemptionsSkipRequestQueue = ShouldRedemptionsSkipRequestQueue ?? false,
            BackgroundColor = ColorTranslator.ToHtml(Color),
        };

    #region EventInput

    /// <summary>
    /// Прямой вызов обработчика (используется тестами и совместимымиconsumer'ами).
    /// </summary>
    public virtual async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rewardEvent);

        if (rewardEvent.Cost != Cost)
            return;

        if (!await Activation.IsActiveAsync(rewardEvent, ct))
            return;

        await OnRewardRedeemedAsync(rewardEvent, ct);
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
        IRewardActivationPolicy activation,
        bool requireFollower = false
    ) =>
        async (rewardEvent, ct) =>
        {
            if (!await activation.IsActiveAsync(rewardEvent, ct))
                return;

            var chain = validator
                .ForRedemption(rewardEvent)
                .RequireBroadcasterUserId()
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

    #endregion
}
