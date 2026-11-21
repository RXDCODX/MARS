namespace MARS.Shared.Contracts.Scoreboard;

public class ScoreboardSnapshot
{
    public ScoreboardPlayer? Player1 { get; set; }
    public ScoreboardPlayer? Player2 { get; set; }
    public ScoreboardMeta? Meta { get; set; }
    public ScoreboardColors? Colors { get; set; }
    public bool IsVisible { get; set; }
    public int AnimationDuration { get; set; }
    public ScoreboardLayoutSnapshot? Layout { get; set; }
}
