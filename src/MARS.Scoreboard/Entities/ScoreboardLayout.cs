namespace MARS.Scoreboard.Entities;

public class ScoreboardLayout
{
    public int Id { get; set; }
    public int HeaderTop { get; set; } = 16;
    public int HeaderLeft { get; set; } = 50;
    public int PlayersTop { get; set; } = 0;
    public int PlayersLeft { get; set; } = 0;
    public int PlayersRight { get; set; } = 0;
    public int HeaderHeight { get; set; } = 60;
    public int HeaderWidth { get; set; } = 400;
    public int PlayerBarHeight { get; set; } = 80;
    public int PlayerBarWidth { get; set; } = 500;
    public int ScoreSize { get; set; } = 60;
    public int FlagSize { get; set; } = 24;
    public int Spacing { get; set; } = 16;
    public int Padding { get; set; } = 16;
    public bool ShowHeader { get; set; } = true;
    public bool ShowFlags { get; set; } = true;
    public bool ShowSponsors { get; set; } = true;
    public bool ShowTags { get; set; } = true;

    public int ScoreboardStateId { get; set; }
    public ScoreboardState ScoreboardState { get; set; } = null!;
}
