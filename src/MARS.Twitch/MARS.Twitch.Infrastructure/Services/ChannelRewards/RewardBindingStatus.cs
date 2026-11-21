namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

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
