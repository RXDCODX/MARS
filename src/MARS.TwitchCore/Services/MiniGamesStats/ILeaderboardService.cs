using MARS.Shared.Models;
using MARS.TwitchCore.Entities;

namespace MARS.TwitchCore.Services.MiniGamesStats;

/// <summary>
/// Таблица лидеров мини-игр: победы в русской рулетке и викторине, раздельно по
/// победам «с женой».
/// </summary>
public interface ILeaderboardService
{
    Task<OperationResult> RecordRouletteWinAsync(
        string twitchId,
        bool withWaifu,
        CancellationToken cancellationToken = default
    );

    Task<OperationResult> RecordTriviaWinAsync(
        string twitchId,
        bool withWaifu,
        CancellationToken cancellationToken = default
    );

    Task<OperationResult<IReadOnlyList<TwitchLeaderboardUser>>> GetTopAsync(
        int count,
        CancellationToken cancellationToken = default
    );

    Task<OperationResult<LeaderboardUserStats>> GetUserStatsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    );
}
