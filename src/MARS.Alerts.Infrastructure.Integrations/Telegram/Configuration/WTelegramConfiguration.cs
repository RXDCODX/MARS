namespace MARS.Alerts.Infrastructure.Integrations.Telegram.Configuration;

public class WTelegramConfiguration
{
    public const string SectionName = "WTelegram";
    public int AppId { get; set; }
    public required string ApiHash { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Password { get; set; }
    public long[] AllowedChannelIds { get; set; } = [];
}
