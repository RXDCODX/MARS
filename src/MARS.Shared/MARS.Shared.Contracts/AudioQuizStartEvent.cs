using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class AudioQuizStartEvent : OverlayEventAbstract {
    public string TrackUrl { get; set; } = "";
    public string? ArtworkUrl { get; set; }
    public int RoundSeconds { get; set; }
}
