using MARS.Shared.Contracts.Media;

namespace MARS.Shared.Contracts;

public class AlertsEvent
{
    public List<MediaPayload> Media { get; set; } = new();
}
