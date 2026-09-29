using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Entities;

public class TelegramDiscordChannelState
{
    [Key]
    public long TelegramChannelId { get; set; }

    public int LastProcessedMessageId { get; set; }

    public DateTime LastUpdatedUtc { get; set; } = DateTime.Now;
}
