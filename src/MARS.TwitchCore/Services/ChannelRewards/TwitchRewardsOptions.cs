namespace MARS.TwitchCore.Services.ChannelRewards;

public class TwitchRewardsOptions
{
    public const string SectionName = "TwitchRewards";

    public Dictionary<int, bool> EnabledByCost { get; set; } = new();

    public int[] ExcludeFromRandomPool { get; set; } = [];
}
