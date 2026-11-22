using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Kernel;

public class WaifuRollEvent : OverlayEventAbstract
{
    public Waifu? Waifu { get; set; }
    public Husband? Husband { get; set; }
}
