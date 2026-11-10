using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Базовый обработчик награды, для которой пока перенесена только маршрутизация.
/// Бизнес-логика намеренно отсутствует: событие фиксируется в журнале и
/// подтверждается, чтобы оно не учитывалось как неизвестная награда.
/// </summary>
public abstract class PendingRewardAlertHandler(ILogger logger, string rewardTitle)
    : RewardHandlerBase
{
    private readonly ILogger _logger = logger;

    /// <summary>Прежнее отображаемое название Twitch-награды.</summary>
    public string RewardTitle { get; } = rewardTitle;

    /// <summary>Ключ, совпадающий с нормализованным названием Twitch-награды.</summary>
    public override string RewardKey => RewardRouting.KeyFor(RewardTitle);

    /// <summary>
    /// Записывает факт вызова незавершённого обработчика, не выполняя механику награды.
    /// </summary>
    protected override Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogWarning(
            "Twitch reward {RewardTitle} ({RewardKey}) was redeemed by {UserName}, "
                + "but its handler is not implemented yet.",
            RewardTitle,
            RewardKey,
            rewardEvent.UserName
        );

        return Task.CompletedTask;
    }
}
