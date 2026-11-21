using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

public sealed class DisabledPolicy : IRewardActivationPolicy
{
    public static DisabledPolicy Instance { get; } = new();

    public ValueTask<bool> IsActiveAsync(
        RewardRedeemedEvent reward,
        CancellationToken cancellationToken
    ) => ValueTask.FromResult(false);
}

