using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts;

public class AdhdConfigEvent : OverlayEventAbstract {
    public AdhdLayoutConfigDto? Config { get; set; }
}
