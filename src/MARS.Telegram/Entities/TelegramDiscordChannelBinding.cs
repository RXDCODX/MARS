using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Entities;

public class TelegramDiscordChannelBinding
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public long TelegramChannelId { get; set; }

    public ulong DiscordChannelId { get; set; }

    public bool IsEnabled { get; set; } = true;

    [MaxLength(500)]
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.Now;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.Now;
}
