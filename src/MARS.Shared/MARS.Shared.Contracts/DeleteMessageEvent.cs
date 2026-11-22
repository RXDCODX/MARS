using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class DeleteMessageEvent : OverlayEventAbstract {
    public string Id { get; set; } = "";
}
