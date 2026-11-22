using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;

public class MikuMikuBeamEvent : OverlayEventAbstract
{
    public List<TwitchUser> Users { get; set; } = new();
}
