using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts;

public class MikuMondayEvent : OverlayEventAbstract {
    public Guid Id { get; set; }
    public TwitchUser? TwitchUser { get; set; }
    public MikuTrackDto? SelectedTrack { get; set; }
    public List<MikuTrackDto>? AvailableTracks { get; set; }
    public bool SkipAvailableTracksUpdate { get; set; }
}
