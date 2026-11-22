using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class AdhdEvent : OverlayEventAbstract {
    public int Seconds { get; set; }
}
