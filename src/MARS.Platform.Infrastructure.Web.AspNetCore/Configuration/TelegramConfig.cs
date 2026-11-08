namespace MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

public class TelegramConfig
{
    public string? BotToken { get; set; }
    public long[]? AdminIds { get; set; }
}
