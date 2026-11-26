namespace MARS.Admin.Application.Services.ServiceManager;

public class ServiceLog
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? ActivityType { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? Platform { get; set; }
    public string? RawMessage { get; set; }
}
