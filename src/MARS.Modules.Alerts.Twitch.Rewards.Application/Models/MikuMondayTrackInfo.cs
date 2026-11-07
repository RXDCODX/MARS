namespace MARS.Modules.Alerts.Twitch.Rewards.Application.Models;

public class MikuMondayTrackInfo
{
    public int Id { get; set; }
    public int Number { get; set; }
    public Guid BaseTrackInfoId { get; set; }
    public string? TrackName { get; set; }
    public string? Artist { get; set; }
    public string? Url { get; set; }
    public string? ArtworkUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
