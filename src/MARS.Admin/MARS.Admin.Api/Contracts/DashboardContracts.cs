namespace MARS.Admin.Api.Contracts;

public class DashboardServiceEntry
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public double? CpuPercent { get; set; }
    public long? MemoryBytes { get; set; }
    public long? GcHeapBytes { get; set; }
    public double? UptimeSeconds { get; set; }
}
