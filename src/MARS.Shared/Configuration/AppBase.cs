namespace MARS.Shared.Configuration;

public class AppBase
{
    public const string SectionName = "AppBase";

    public TwitchConfig? Twitch { get; set; }
    public TelegramConfig? Telegram { get; set; }
    public DiscordConfig? Discord { get; set; }
}

public class TwitchConfig
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

public class TelegramConfig
{
    public string? BotToken { get; set; }
    public long[]? AdminIds { get; set; }
}

public class DiscordConfig
{
    public string? BotToken { get; set; }
}
