using MARS.Shared.Contracts.Rewards;

namespace MARS.Twitch.Api.Services.ChannelRewards;

public sealed record RemoteReward(string Id, string Title, int Cost);

public enum RewardBindingStatus
{
    Bound,
    AlreadyBound,
    Rebound,
    CostDrift,
    Orphaned,
    NeedsManualBinding,
    Conflicting,
}

public sealed record RewardBindingResult(
    string Code,
    RewardBindingStatus Status,
    string? TwitchRewardId,
    string? Reason
);

public sealed record RewardBindingReport(IReadOnlyList<RewardBindingResult> Results);
