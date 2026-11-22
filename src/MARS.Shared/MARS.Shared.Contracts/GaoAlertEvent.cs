using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models;

namespace MARS.Shared.Contracts;

public class GaoAlertEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TwitchUser? TwitchUser { get; set; }
    public bool IsJustText { get; set; }
    public string? JustText { get; set; }
}
