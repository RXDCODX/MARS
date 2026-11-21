namespace MARS.Shared.Contracts.Scoreboard;

public class ScoreboardEvent
{
    public ScoreboardSnapshot? StateUpdated { get; set; }
    public PlayerScoreUpdated? PlayerScoreUpdated { get; set; }
    public PlayerFinalUpdated? PlayerFinalUpdated { get; set; }
    public VisibilityChanged? VisibilityChanged { get; set; }

    /// <summary>Имя заполненной ветки; раньше — enum и цепочка из четырёх проверок.</summary>
    public string? EventCase => OneofEventContract.GetActiveBranch(this)?.Name;
}
