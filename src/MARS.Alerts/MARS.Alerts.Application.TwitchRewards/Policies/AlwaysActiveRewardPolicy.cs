using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.TwitchRewards;

public sealed class AlwaysActiveRewardPolicy : IRewardActivationPolicy
{
    public static AlwaysActiveRewardPolicy Instance { get; } = new();

    public ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken
    ) => ValueTask.FromResult(true);
}