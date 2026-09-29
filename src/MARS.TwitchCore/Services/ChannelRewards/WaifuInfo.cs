namespace MARS.TwitchCore.Services.ChannelRewards;

/// <summary>
/// Minimal waifu info needed for reward answer formatting.
/// </summary>
public class WaifuInfo
{
    public required string Name { get; init; }
    public string? Anime { get; init; }
    public string? Manga { get; init; }
}
