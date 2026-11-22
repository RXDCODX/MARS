using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Kernel;

public class FumoFridayEvent : OverlayEventAbstract
{
    public TwitchUser? User { get; set; }
}
