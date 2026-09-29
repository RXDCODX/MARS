using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.Telegram.Entities;

public class TelegramUser
{
    public required string Name { get; set; }

    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long UserId { get; set; }

    public DateTime LastTimeMessage { get; set; }

    public bool RaidHelper { get; set; } = false;
    public bool PyroAlertsAccess { get; set; } = false;
    public bool IsRandomMemeSendler { get; set; } = false;
    public bool HonkaiNotifications { get; set; } = false;
    public bool StreamUpNotifications { get; set; } = false;
    public bool ZenlessZoneZeroDailyNotif { get; set; } = false;
    public bool GenshinImpactDailyNotif { get; set; } = false;
    public DateTime ByeByeLastMessageTime { get; set; } = DateTime.Now;
    public bool ByeByeServiceNotification { get; set; } = false;
}
