using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class HighliteEvent : OverlayEventAbstract {
    public string Message { get; set; } = "";
    public string Color { get; set; } = "";
    public string? FaceUrl { get; set; }
}
