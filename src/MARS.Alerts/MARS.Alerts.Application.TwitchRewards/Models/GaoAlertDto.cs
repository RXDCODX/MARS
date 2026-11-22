using MARS.Shared.Kernel;

namespace MARS.Alerts.Application.TwitchRewards.Models;

public class GaoAlertDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TwitchUser? TwitchUser { get; set; }
    public bool IsJustText { get; set; }
    public string? JustText { get; set; }
}
