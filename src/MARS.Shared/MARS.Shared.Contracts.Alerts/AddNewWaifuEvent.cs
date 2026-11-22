using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Kernel;

public class AddNewWaifuEvent : OverlayEventAbstract
{
    public Waifu? Waifu { get; set; }
    public TwitchUser? User { get; set; }
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}
