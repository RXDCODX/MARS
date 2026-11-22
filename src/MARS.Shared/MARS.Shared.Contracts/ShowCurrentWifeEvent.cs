using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class ShowCurrentWifeEvent : OverlayEventAbstract {
    public WaifuInfo? Waifu { get; set; }
    public HusbandInfo? Husband { get; set; }
    public string Avatar { get; set; } = "";
    public string Color { get; set; } = "";
}
