namespace MARS.Admin.Api.Contracts;

public class DashboardActivityStatsResponse
{
    public int Total { get; set; }

    public int Warnings { get; set; }

    public int Errors { get; set; }

    public Dictionary<string, int> ByAction { get; set; } = new();

    public Dictionary<string, int> ByService { get; set; } = new();

    public Dictionary<string, int> ByPlatform { get; set; } = new();

    public List<DashboardActivityTimelinePoint> Timeline { get; set; } = [];

    public List<DashboardActivityActor> TopActors { get; set; } = [];
}
