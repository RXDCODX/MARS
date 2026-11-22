using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class ScreenEmojisParticlesEvent : OverlayEventAbstract {
    public string Message { get; set; } = "";
}
