namespace MARS.Shared.Contracts.Scoreboard;

using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using MARS.Shared.Contracts.Scoreboard;
using MARS.Shared.Kernel;

public class ScoreboardEvent : OverlayEventAbstract
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
