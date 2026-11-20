namespace MARS.Shared.Contracts.Scoreboard;

public class ScoreboardPlayer
{
    public string Name { get; set; } = "";
    public string Sponsor { get; set; } = "";
    public int Score { get; set; }
    public string Tag { get; set; } = "";
    public string Flag { get; set; } = "";
    public string Final { get; set; } = "";
}

public class ScoreboardMeta
{
    public string Title { get; set; } = "";
    public string FightRule { get; set; } = "";
}

public class ScoreboardColors
{
    public string MainColor { get; set; } = "";
    public string PlayerNamesColor { get; set; } = "";
    public string TournamentTitleColor { get; set; } = "";
    public string FightModeColor { get; set; } = "";
    public string ScoreColor { get; set; } = "";
    public string BackgroundColor { get; set; } = "";
    public string BorderColor { get; set; } = "";
}

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

public class PlayerScoreUpdated
{
    public int PlayerPosition { get; set; }
    public int NewScore { get; set; }
}

public class PlayerFinalUpdated
{
    public int PlayerPosition { get; set; }
    public string Final { get; set; } = "";
}

public class VisibilityChanged
{
    public bool IsVisible { get; set; }
}

public class ScoreboardEvent
{
    public ScoreboardSnapshot? StateUpdated { get; set; }
    public PlayerScoreUpdated? PlayerScoreUpdated { get; set; }
    public PlayerFinalUpdated? PlayerFinalUpdated { get; set; }
    public VisibilityChanged? VisibilityChanged { get; set; }

    public enum EventOneofCase
    {
        None = 0,
        StateUpdated = 1,
        PlayerScoreUpdated = 2,
        PlayerFinalUpdated = 3,
        VisibilityChanged = 4,
    }

    public EventOneofCase EventCase
    {
        get
        {
            if (StateUpdated is not null)
                return EventOneofCase.StateUpdated;
            if (PlayerScoreUpdated is not null)
                return EventOneofCase.PlayerScoreUpdated;
            if (PlayerFinalUpdated is not null)
                return EventOneofCase.PlayerFinalUpdated;
            if (VisibilityChanged is not null)
                return EventOneofCase.VisibilityChanged;
            return EventOneofCase.None;
        }
    }
}
