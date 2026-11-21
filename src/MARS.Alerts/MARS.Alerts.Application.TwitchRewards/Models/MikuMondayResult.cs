using MARS.Shared.Contracts.Models;

namespace MARS.Alerts.Application.Twitch.Rewards.Models;

public class MikuMondayResult
{
    public MikuMondayTrackInfo? Track { get; set; }
    public List<MikuMondayTrackInfo> AvailableTracks { get; set; } = [];
    public string? Error { get; set; }
    public bool IsSuccess => Track != null && string.IsNullOrWhiteSpace(Error);
}
