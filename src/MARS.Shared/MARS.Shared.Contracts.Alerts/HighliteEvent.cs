namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

public class HighliteEvent : OverlayEventAbstract
{
    public string Message { get; set; } = "";
    public string Color { get; set; } = "";
    public string? FaceUrl { get; set; }
}
