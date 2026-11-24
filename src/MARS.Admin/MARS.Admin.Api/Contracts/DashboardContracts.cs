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

public class DashboardInfraEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
}

public class DashboardOverviewResponse
{
    public List<DashboardServiceEntry> Services { get; set; } = [];
    public List<DashboardInfraEntry> Infrastructure { get; set; } = [];
    public int ActiveAlerts { get; set; }
    public bool MetricsAvailable { get; set; }
    public Dictionary<string, bool> Connections { get; set; } = new();
}

public class DashboardEventDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SourceService { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? ActorDisplayName { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public class DashboardEventStatsResponse
{
    public int Total { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public List<(string Actor, int Count)> TopActors { get; set; } = [];
}
