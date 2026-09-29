using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Entities;

public class ChannelProcessingState
{
    [Key]
    public long ChannelId { get; set; }

    public int OffsetId { get; set; }

    public long? MessagesHash { get; set; }

    public DateTime LastUpdated { get; set; }
}
