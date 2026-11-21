using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards;

public interface IRewardActivationPolicy
{
    ValueTask<bool> IsActiveAsync(RewardRedeemedEvent reward, CancellationToken cancellationToken);
}

