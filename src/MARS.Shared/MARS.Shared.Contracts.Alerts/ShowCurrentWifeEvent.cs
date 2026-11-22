using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

public class ShowCurrentWifeEvent : OverlayEventAbstract
{
    public Waifu? Waifu { get; set; }
    public HusbandInfo? Husband { get; set; }
    public string Avatar { get; set; } = "";
    public string Color { get; set; } = "";
}
