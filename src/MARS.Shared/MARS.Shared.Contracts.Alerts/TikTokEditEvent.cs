namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

public class TikTokEditEvent : OverlayEventAbstract
{
    public string Guid { get; set; } = "";
    public string Text { get; set; } = "";
}
