namespace MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

public class AppBase
{
    public const string SectionName = "AppBase";

    public TwitchConfig? Twitch { get; set; }
    public TelegramConfig? Telegram { get; set; }
    public DiscordConfig? Discord { get; set; }
}
