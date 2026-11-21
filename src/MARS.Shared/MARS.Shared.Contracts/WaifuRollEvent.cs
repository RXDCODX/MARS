using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class WaifuRollEvent
{
    public WaifuInfo? Waifu { get; set; }
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}
