namespace MARS.Admin.Api.Contracts;

public class DashboardEventDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SourceService { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? ActorDisplayName { get; set; }
    public string PayloadJson { get; set; } = "{}";
}
