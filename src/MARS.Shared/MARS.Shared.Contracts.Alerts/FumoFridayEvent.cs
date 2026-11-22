namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

public class FumoFridayEvent : OverlayEventAbstract
{
    public string DisplayName { get; set; } = "";
    public string Color { get; set; } = "";
}
