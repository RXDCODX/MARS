using MARS.Shared.Contracts.Media;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts;

public class ShowCurrentWifeEvent : OverlayEventAbstract
{
    public Waifu? Waifu { get; set; }
    public HusbandInfo? Husband { get; set; }
    public string Avatar { get; set; } = "";
    public string Color { get; set; } = "";
}
