using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда «Вернуть всем»: клиент показывает экран возврата конкретному
/// зрителю, поэтому пользователь едет в данных.
/// </summary>
public sealed class AllRefundEffect : IRewardActivationEffect
{
    public string AlertKey => RewardAlertKeys.AllRefund;

    public Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        var result = RewardEffectResult.Suppressed;

        if (context.Reward.User is not null)
        {
            result = new RewardEffectResult { Data = context.Reward.User };
        }

        return Task.FromResult(result);
    }
}
