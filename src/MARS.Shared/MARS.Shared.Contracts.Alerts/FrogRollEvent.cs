namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Kernel;

public class FrogRollEvent : OverlayEventAbstract
{
    public string FrogJson { get; set; } = "";
    public string TwitchUserJson { get; set; } = "";
    public string Color { get; set; } = "";
}
