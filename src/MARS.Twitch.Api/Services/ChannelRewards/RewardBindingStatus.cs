namespace MARS.Twitch.Api.Services.ChannelRewards;

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
