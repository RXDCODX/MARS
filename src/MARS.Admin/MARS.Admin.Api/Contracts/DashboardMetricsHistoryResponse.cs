namespace MARS.Admin.Api.Contracts;

public class DashboardMetricsHistoryResponse
{
    public List<DashboardMetricsPoint> Points { get; set; } = [];
}

public class DashboardMetricsPoint
{
    public DateTime TimestampUtc { get; set; }

    public double? CpuPercent { get; set; }

    public long? MemoryBytes { get; set; }

    public long? GcHeapBytes { get; set; }
}
