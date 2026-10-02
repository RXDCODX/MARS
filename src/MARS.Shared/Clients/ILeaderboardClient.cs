using MARS.Shared.Clients;

namespace MARS.Shared.Clients;

/// <summary>
/// Таблица лидеров мини-игр во внутреннем API MARS.TwitchCore.
/// </summary>
public interface ILeaderboardClient
{
    /// <summary>
    /// Топ игроков. null означает «сервис недоступен или ответил ошибкой» —
    /// это не то же самое, что пустой топ.
    /// </summary>
    Task<LeaderboardTop?> GetTopAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Статистика одного игрока. null — сервис недоступен или ответил ошибкой.
    /// </summary>
    Task<LeaderboardStats?> GetUserStatsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    );
}
