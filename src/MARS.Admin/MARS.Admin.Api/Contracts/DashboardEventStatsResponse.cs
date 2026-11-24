namespace MARS.Admin.Api.Contracts;

public class DashboardEventStatsResponse
{
    public int Total { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public List<(string Actor, int Count)> TopActors { get; set; } = [];
}
