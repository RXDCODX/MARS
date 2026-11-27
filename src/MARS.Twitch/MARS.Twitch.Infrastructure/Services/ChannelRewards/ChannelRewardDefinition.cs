using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public class ChannelRewardDefinition : ITwitchReward
{
    public required string Title { get; init; }
    public required int Cost { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? Prompt { get; init; }
    public string? BackgroundColor { get; init; } = "#9146FF";
    public bool IsUserInputRequired { get; init; } = false;
    public string? RewardCode { get; init; }
    public bool? IsMaxPerStreamEnabled { get; init; }
    public int? MaxPerStream { get; init; }
    public bool? IsMaxPerUserPerStreamEnabled { get; init; }
    public int? MaxPerUserPerStream { get; init; }
    public bool? IsGlobalCooldownEnabled { get; init; }
    public int? GlobalCooldownSeconds { get; init; }
    public bool? ShouldRedemptionsSkipRequestQueue { get; init; }

    public virtual CreateCustomRewardsRequest ToCreateRequest()
    {
        return new CreateCustomRewardsRequest
        {
            Title = Title,
            Cost = Cost,
            IsEnabled = IsEnabled,
            Prompt = Prompt,
            BackgroundColor = BackgroundColor,
            IsUserInputRequired = IsUserInputRequired,
            IsMaxPerStreamEnabled = IsMaxPerStreamEnabled ?? false,
            MaxPerStream = MaxPerStream,
            IsMaxPerUserPerStreamEnabled = IsMaxPerUserPerStreamEnabled ?? false,
            MaxPerUserPerStream = MaxPerUserPerStream,
            IsGlobalCooldownEnabled = IsGlobalCooldownEnabled ?? false,
            GlobalCooldownSeconds = GlobalCooldownSeconds,
            ShouldRedemptionsSkipRequestQueue = ShouldRedemptionsSkipRequestQueue ?? false,
        };
    }
}
