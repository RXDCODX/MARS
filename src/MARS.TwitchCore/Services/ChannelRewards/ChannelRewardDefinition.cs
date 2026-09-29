using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace MARS.TwitchCore.Services.ChannelRewards;

public abstract class ChannelRewardDefinition : ITwitchReward
{
    public required string Title { get; init; }
    public required int Cost { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? Prompt { get; init; }
    public string? BackgroundColor { get; init; } = "#9146FF";
    public bool IsUserInputRequired { get; init; } = false;
    public bool IsMaxPerStreamEnabled { get; init; } = false;
    public int? MaxPerStream { get; init; }
    public bool IsMaxPerUserPerStreamEnabled { get; init; } = false;
    public int? MaxPerUserPerStream { get; init; }
    public bool IsGlobalCooldownEnabled { get; init; } = false;
    public int? GlobalCooldownSeconds { get; init; }
    public bool ShouldRedemptionsSkipRequestQueue { get; init; } = false;

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
            IsMaxPerStreamEnabled = IsMaxPerStreamEnabled,
            MaxPerStream = MaxPerStream,
            IsMaxPerUserPerStreamEnabled = IsMaxPerUserPerStreamEnabled,
            MaxPerUserPerStream = MaxPerUserPerStream,
            IsGlobalCooldownEnabled = IsGlobalCooldownEnabled,
            GlobalCooldownSeconds = GlobalCooldownSeconds,
            ShouldRedemptionsSkipRequestQueue = ShouldRedemptionsSkipRequestQueue,
        };
    }
}

public sealed class PyroAlertRewardDefinition : ChannelRewardDefinition
{
    public Guid? MediaInfoId { get; init; }
}

public interface ITwitchReward
{
    int Cost { get; init; }
}
