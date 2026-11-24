namespace MARS.Admin.Api.Contracts;

public class DashboardOverviewResponse
{
    public List<DashboardServiceEntry> Services { get; set; } = [];
    public List<DashboardInfraEntry> Infrastructure { get; set; } = [];
    public int ActiveAlerts { get; set; }
    public bool MetricsAvailable { get; set; }
    public Dictionary<string, bool> Connections { get; set; } = new();
}
