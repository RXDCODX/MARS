namespace MARS.Shared.Contracts.Clients;

/// <summary>
/// Место пользователя в таблице вместе с его строкой.
/// </summary>
public sealed record LeaderboardStats(int Place, LeaderboardEntry? User);
