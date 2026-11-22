using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class ScreenParticlesEvent : OverlayEventAbstract {
    public string Particles { get; set; } = "";
}
