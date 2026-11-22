using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Messaging;

namespace MARS.Shared.Contracts;

public class NewMessageEvent : OverlayEventAbstract {
    public string Id { get; set; } = "";
    public ChatEventMessageEvent? Message { get; set; }
}
