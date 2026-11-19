namespace MARS.Twitch.Api.Services.ChannelRewards;

public sealed record RewardBindingResult(
    string Code,
    RewardBindingStatus Status,
    string? TwitchRewardId,
    string? Reason
);
