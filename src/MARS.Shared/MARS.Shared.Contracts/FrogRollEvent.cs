using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class FrogRollEvent : OverlayEventAbstract {
    public string FrogJson { get; set; } = "";
    public string TwitchUserJson { get; set; } = "";
    public string Color { get; set; } = "";
}
