namespace MARS.Alerts.Models;

public class GaoAlertDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public object? TwitchUser { get; set; }
    public bool IsJustText { get; set; }
    public string? JustText { get; set; }
}
