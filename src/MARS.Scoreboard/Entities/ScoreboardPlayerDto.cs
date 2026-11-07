namespace MARS.Scoreboard.Entities;

public class ScoreboardPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public string Sponsor { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public string Final { get; set; } = "none";
}
