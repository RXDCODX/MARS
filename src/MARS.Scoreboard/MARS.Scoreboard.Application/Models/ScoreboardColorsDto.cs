using MARS.Shared.Contracts.Scoreboard;

namespace MARS.Scoreboard.Application.Models;

public class ScoreboardColorsDto
{
    public string MainColor { get; set; } = "#3F00FF";
    public string PlayerNamesColor { get; set; } = "#FFFFFF";
    public string TournamentTitleColor { get; set; } = "#FFFFFF";
    public string FightModeColor { get; set; } = "#FFFFFF";
    public string ScoreColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "rgba(0, 0, 0, 0.8)";
    public string BorderColor { get; set; } = "#3F00FF";
}
