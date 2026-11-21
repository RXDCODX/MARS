using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

public interface IRewardActivationPolicy
{
    ValueTask<bool> IsActiveAsync(RewardRedeemedEvent reward, CancellationToken cancellationToken);
}

