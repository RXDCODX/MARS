namespace MARS.Twitch.Infrastructure.Services.ChannelRewards;

public class TwitchRewardsOptions
{
    public const string SectionName = "TwitchRewards";

    public int SyncIntervalMinutes { get; set; } = 5;

    public int[] ExcludeFromRandomPool { get; set; } = [];
}
