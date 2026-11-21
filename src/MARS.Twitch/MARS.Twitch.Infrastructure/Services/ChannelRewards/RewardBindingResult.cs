namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public sealed record RewardBindingResult(
    string Code,
    RewardBindingStatus Status,
    string? TwitchRewardId,
    string? Reason
);
