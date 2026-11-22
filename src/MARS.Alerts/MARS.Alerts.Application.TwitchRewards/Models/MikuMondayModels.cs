using MARS.Shared.Contracts.Models;
using MARS.Shared.Kernel;

namespace MARS.Alerts.Application.TwitchRewards.Models;

public class MikuMondayDto
{
    public Guid Id { get; set; }
    public required TwitchUser TwitchUser { get; set; }
    public required MikuTrackDto SelectedTrack { get; set; }
    public required List<MikuTrackDto> AvailableTracks { get; set; }
    public bool SkipAvailableTracksUpdate { get; set; }
}
