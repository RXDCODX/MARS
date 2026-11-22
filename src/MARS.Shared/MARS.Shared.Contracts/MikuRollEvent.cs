using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class MikuRollEvent : OverlayEventAbstract {
    public string MikuModuleJson { get; set; } = "";
    public string TwitchUserJson { get; set; } = "";
    public string Color { get; set; } = "";
    public int CollectedCount { get; set; }
    public int TotalCount { get; set; }
}
