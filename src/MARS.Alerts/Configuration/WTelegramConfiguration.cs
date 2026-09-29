namespace MARS.Alerts.Configuration;

public class WTelegramConfiguration
{
    public static readonly string SectionName = "WTelegram";
    public int AppId { get; set; }
    public required string ApiHash { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Password { get; set; }
    public long[] AllowedChannelIds { get; set; } = [];
}
