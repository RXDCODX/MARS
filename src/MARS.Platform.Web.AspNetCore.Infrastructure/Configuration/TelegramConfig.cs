namespace MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;

public class TelegramConfig
{
    public string? BotToken { get; set; }
    public long[]? AdminIds { get; set; }
}
