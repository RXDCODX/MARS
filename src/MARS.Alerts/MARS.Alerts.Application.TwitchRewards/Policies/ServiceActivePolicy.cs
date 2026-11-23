using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards.Policies;

public sealed class ServiceActivePolicy(Func<bool> isActive) : IRewardActivationPolicy
{
    public ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken
    ) => ValueTask.FromResult(isActive());
}
