namespace MARS.TwitchCore.Configuration;

public class TelegramConfiguration
{
    public static readonly string TelegramSection = "Telegram";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    public long[] AdminIdsArray { get; set; }
}
