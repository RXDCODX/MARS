using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.TwitchCore.Services.MiniGamesStats;
using Microsoft.AspNetCore.Mvc;

namespace MARS.TwitchCore.Controllers;

/// <summary>
/// Внутренний API таблицы лидеров мини-игр.
/// </summary>
/// <remarks>
/// Маршрут ложится под уже существующее правило YARP <c>twitch-core-stats</c>,
/// поэтому новое свойство в <c>ServiceEndpoints</c> и новое правило в
/// <c>Yarp:Routes</c> не понадобились.
/// </remarks>
[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController(ILeaderboardService leaderboardService) : ControllerBase
{
    /// <summary>Сколько строк возвращать по умолчанию.</summary>
    private const int DefaultTopCount = 3;

    [HttpGet("top")]
    public async Task<ActionResult<OperationResult<LeaderboardTop>>> GetTop(
        [FromQuery] int count = DefaultTopCount
    )
    {
        var result = await leaderboardService.GetTopAsync(count, HttpContext.RequestAborted);

        return Ok(
            result.Success && result.Result is not null
                ? OperationResult<LeaderboardTop>.Ok(
                    new LeaderboardTop(result.Result.Select(user => ToEntry(user)!).ToArray())
                )
                : OperationResult<LeaderboardTop>.Fail(
                    result.ErrorMessage ?? "Не удалось получить таблицу лидеров"
                )
        );
    }

    [HttpGet("user/{twitchId}")]
    public async Task<ActionResult<OperationResult<LeaderboardStats>>> GetUser(
        [FromRoute] string twitchId
    )
    {
        var result = await leaderboardService.GetUserStatsAsync(
            twitchId,
            HttpContext.RequestAborted
        );

        return Ok(
            result.Success && result.Result is not null
                ? OperationResult<LeaderboardStats>.Ok(
                    new LeaderboardStats(result.Result.Place, ToEntry(result.Result.User))
                )
                : OperationResult<LeaderboardStats>.Fail(
                    result.ErrorMessage ?? "Не удалось получить статистику игрока"
                )
        );
    }

    private static LeaderboardEntry? ToEntry(TwitchCore.Entities.TwitchLeaderboardUser? user)
    {
        if (user is null)
        {
            return null;
        }

        return new LeaderboardEntry(
            user.TwitchId,
            user.TwitchUser?.DisplayName,
            user.TotalWins,
            user.RussianRouletteWins,
            user.TriviaWins
        );
    }
}
