namespace MARS.Telegram.Application.Models;

public class TelegramDiscordChannelStateDto
{
    public long TelegramChannelId { get; set; }
    public int LastProcessedMessageId { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
