using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Базовый обработчик награды, для которой в этом этапе перенесена только маршрутизация.
/// Бизнес-логика намеренно не реализована: legacy-механика (кулдауны, дни недели,
/// исключения из пула, ввод/refund, мини-игры) классифицирована как отложенная,
/// а событие фиксируется и подтверждается, чтобы оно не учитывалось как неизвестная награда.
/// </summary>
public abstract class PendingRewardAlertHandler(ILogger logger, string rewardCode)
    : RewardHandlerBase
{
    private readonly ILogger _logger = logger;

    public override string RewardCode { get; } = rewardCode;

    public string RewardTitle =>
        MARS.Shared.Contracts.Rewards.RewardCatalog.Get(RewardCode).LegacyTitle;

    protected override Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogWarning(
            "Twitch reward {RewardTitle} ({RewardCode}) was redeemed by {UserName}, "
                + "but its handler is not implemented yet.",
            RewardTitle,
            RewardCode,
            rewardEvent.UserName
        );

        return Task.CompletedTask;
    }
}
