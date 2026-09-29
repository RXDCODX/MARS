using MARS.Scoreboard.Entities;

namespace MARS.Scoreboard.Hubs.Interfaces;

public interface IScoreboardHub
{
    Task ReceiveState(ScoreboardDto state);
    Task StateUpdated(ScoreboardDto state);
    Task PlayerScoreUpdated(int playerPosition, int newScore);
    Task PlayerFinalUpdated(int playerPosition, string final);
    Task VisibilityChanged(bool isVisible);
}
