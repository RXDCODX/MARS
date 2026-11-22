using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts;

public class AllRefundEvent : OverlayEventAbstract {
    public TwitchUser? User { get; set; }
}
