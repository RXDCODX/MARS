using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class AlertEvent : OverlayEventAbstract {
    public MediaPayload? Media { get; set; }
}
