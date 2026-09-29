namespace MARS.Admin.Entities;

public class ServiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ServiceStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? LastActivity { get; set; }
    public bool IsEnabled { get; set; }
    public string? HealthEndpoint { get; set; }
}
