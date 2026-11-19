using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

public sealed class CompositeRewardPolicy(params IRewardActivationPolicy[] policies)
    : IRewardActivationPolicy
{
    public async ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken
    )
    {
        foreach (var policy in policies)
        {
            if (!await policy.IsActiveAsync(reward, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }
}
