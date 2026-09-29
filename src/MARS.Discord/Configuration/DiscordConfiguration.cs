namespace MARS.Discord.Configuration;

public class DiscordConfiguration
{
    public static readonly string Configuration = "DiscordConfig";

    public string Token { get; set; } = string.Empty;

    public ulong[] AdminIdsArray { get; set; } = [];
}
