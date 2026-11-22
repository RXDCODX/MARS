using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Kernel;

public class MikuRollEvent : OverlayEventAbstract
{
    public MikuModule? Module { get; set; }
    public TwitchUser? User { get; set; }
    public int CollectedCount { get; set; }
    public int TotalCount { get; set; }
}
