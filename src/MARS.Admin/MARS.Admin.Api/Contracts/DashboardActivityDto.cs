namespace MARS.Admin.Api.Contracts;

public class DashboardActivityDto
{
    public DateTime OccurredAtUtc { get; set; }

    public string Level { get; set; } = "Information";

    public string Service { get; set; } = string.Empty;

    public string? ActivityType { get; set; }

    public string? Platform { get; set; }

    public string? ActorDisplayName { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? RawMessage { get; set; }
}
