using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class AlertsEvent : OverlayEventAbstract {
    public List<MediaPayload> Media { get; set; } = new();
}
