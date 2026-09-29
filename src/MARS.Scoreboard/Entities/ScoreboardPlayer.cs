namespace MARS.Scoreboard.Entities;

public class ScoreboardPlayer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sponsor { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public string Final { get; set; } = "none";
    public int Position { get; set; }
    public int ScoreboardStateId { get; set; }
    public ScoreboardState ScoreboardState { get; set; } = null!;
}
