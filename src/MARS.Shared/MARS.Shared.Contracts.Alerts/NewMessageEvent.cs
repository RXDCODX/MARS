using MARS.Shared.Contracts.Messaging;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

public class NewMessageEvent : OverlayEventAbstract
{
    public string Id { get; set; } = "";
    public ChatEventMessageEvent? Message { get; set; }
}
