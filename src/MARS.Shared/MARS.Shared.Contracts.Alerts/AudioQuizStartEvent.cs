namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

public class AudioQuizStartEvent : OverlayEventAbstract
{
    public string TrackUrl { get; set; } = "";
    public string? ArtworkUrl { get; set; }
    public int RoundSeconds { get; set; }
}
