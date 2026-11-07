namespace MARS.Modules.Alerts.Twitch.Rewards.Application.Models;

public class MikuMondayActivationInfo
{
    public int Id { get; set; }
    public required string TwitchUserId { get; set; }
    public required string DisplayName { get; set; }
    public int MikuMondayTrackId { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.Now;
    public int WeekOfYear { get; set; }
    public int Year { get; set; }
}
