using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class TikTokEditEvent : OverlayEventAbstract {
    public string Guid { get; set; } = "";
    public string Text { get; set; } = "";
}
