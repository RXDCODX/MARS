using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class AutoMessageEvent : OverlayEventAbstract {
    public string Message { get; set; } = "";
}
