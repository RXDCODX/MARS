using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Policies;

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

    public async ValueTask<bool> IsActiveForSchedulingAsync(CancellationToken cancellationToken)
    {
        foreach (var policy in policies.Where(p => !p.IsStateful))
        {
            if (!await policy.IsActiveForSchedulingAsync(cancellationToken))
            {
                return false;
            }
        }

        return true;
    }
}
