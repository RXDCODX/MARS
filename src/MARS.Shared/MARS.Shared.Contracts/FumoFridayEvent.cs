using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class FumoFridayEvent : OverlayEventAbstract {
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}
