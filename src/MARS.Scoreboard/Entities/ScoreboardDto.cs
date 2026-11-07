namespace MARS.Scoreboard.Entities;

public class ScoreboardDto
{
    public ScoreboardPlayerDto Player1 { get; set; } = new();
    public ScoreboardPlayerDto Player2 { get; set; } = new();
    public ScoreboardMetaDto Meta { get; set; } = new();
    public ScoreboardColorsDto Colors { get; set; } = new();
    public bool IsVisible { get; set; } = true;
    public int AnimationDuration { get; set; } = 800;
    public ScoreboardLayoutDto? Layout { get; set; }
}
