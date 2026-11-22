using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Kernel;

public class FrogRollEvent : OverlayEventAbstract
{
    public Frog? Frog { get; set; }
    public TwitchUser? User { get; set; }
}
