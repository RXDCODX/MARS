using System.Data.SqlTypes;
using System.Drawing;
using MARS.Alerts.Application.TwitchRewards;
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
    public abstract int RewardCost { get; }
    public abstract string AlertDisplayName { get; set; }
    public abstract string AlertDescription { get; set; }
    public abstract Color Color { get; set; }
    public abstract int Cost { get; init; }
    public abstract Func<bool> IsRewardEnabled { get; set; }
    public virtual Guid? TwitchRewardId { get; private protected set; }
    public abstract Task StartAsync(CancellationToken cancellationToken);

    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public virtual IRewardActivationPolicy Activation => AlwaysActiveRewardPolicy.Instance;
}
