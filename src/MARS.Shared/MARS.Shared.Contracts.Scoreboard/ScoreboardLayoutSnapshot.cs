namespace MARS.Shared.Contracts.Scoreboard;

public class ScoreboardLayoutSnapshot
{
    public int HeaderTop { get; set; }
    public int HeaderLeft { get; set; }
    public int PlayersTop { get; set; }
    public int PlayersLeft { get; set; }
    public int PlayersRight { get; set; }
    public int HeaderHeight { get; set; }
    public int HeaderWidth { get; set; }
    public int PlayerBarHeight { get; set; }
    public int PlayerBarWidth { get; set; }
    public int ScoreSize { get; set; }
    public int FlagSize { get; set; }
    public int Spacing { get; set; }
    public int Padding { get; set; }
    public bool ShowHeader { get; set; }
    public bool ShowFlags { get; set; }
    public bool ShowSponsors { get; set; }
    public bool ShowTags { get; set; }
}
