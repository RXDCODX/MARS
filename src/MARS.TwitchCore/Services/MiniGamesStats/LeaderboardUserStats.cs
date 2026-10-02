using MARS.TwitchCore.Entities;

namespace MARS.TwitchCore.Services.MiniGamesStats;

/// <summary>
/// Место пользователя в таблице лидеров вместе с его строкой.
/// </summary>
/// <remarks>
/// Отдельный тип вместо кортежа: пара «место + строка» возвращается из одного
/// метода и разбирается в команде <c>mgleaders</c>, а безымянный кортеж там
/// читался бы как <c>result.Item1</c>.
/// </remarks>
public sealed record LeaderboardUserStats(int Place, TwitchLeaderboardUser? User);
