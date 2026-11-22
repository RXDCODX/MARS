using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class PostTwitchInfoEvent : OverlayEventAbstract {
    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
}
