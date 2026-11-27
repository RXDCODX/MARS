namespace MARS.Admin.Api.Contracts;

public class DashboardMetricsPoint
{
    public DateTime TimestampUtc { get; set; }

    public double? CpuPercent { get; set; }

    public long? MemoryBytes { get; set; }

    public long? GcHeapBytes { get; set; }
}
