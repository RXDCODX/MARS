namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

public class PostTwitchInfoEvent : OverlayEventAbstract
{
    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
}
