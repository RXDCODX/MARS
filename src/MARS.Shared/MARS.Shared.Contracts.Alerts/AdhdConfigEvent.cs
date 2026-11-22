using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Alerts;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Kernel;

public class AdhdConfigEvent : OverlayEventAbstract
{
    public AdhdLayoutConfigDto? Config { get; set; }
}
