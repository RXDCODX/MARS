namespace MARS.Admin.Entities;

public class ServiceState
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsServiceActive { get; set; } = true;
    public ServiceStatus Status { get; set; } = ServiceStatus.Stopped;
    public DateTime? LastStartTime { get; set; }
    public DateTime? LastActivity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
