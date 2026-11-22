using MARS.Shared.Contracts.Media;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts;

public class AddNewWaifuEvent : OverlayEventAbstract
{
    public Waifu? Waifu { get; set; }
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}
