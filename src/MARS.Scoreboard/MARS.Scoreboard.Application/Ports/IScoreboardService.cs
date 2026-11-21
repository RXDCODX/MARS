using MARS.Scoreboard.Application.Models;

namespace MARS.Scoreboard.Application.Ports;

public interface IScoreboardService
{
    Task<ScoreboardDto> GetCurrentStateAsync();
    Task UpdateStateAsync(ScoreboardDto? dto);
    Task<bool> SetVisibilityAsync(bool isVisible);
    Task<bool> UpdatePlayerScoreAsync(int playerPosition, int newScore);
    Task<bool> SetPlayerFinalAsync(int playerPosition, string final);
}