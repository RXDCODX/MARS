using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Kernel;

public class MikuMondayEvent : OverlayEventAbstract
{
    public Guid Id { get; set; }
    public TwitchUser? TwitchUser { get; set; }
    public MikuTrackDto? SelectedTrack { get; set; }
    public List<MikuTrackDto>? AvailableTracks { get; set; }
    public bool SkipAvailableTracksUpdate { get; set; }
}
