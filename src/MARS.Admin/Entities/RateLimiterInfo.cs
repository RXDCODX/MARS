namespace MARS.Admin.Entities;

/// <summary>
/// Информация о состоянии рейт лимитера Shikimori API
/// </summary>
public record RateLimiterInfo
{
    public int AvailablePerSecond { get; init; }
    public int AvailablePerMinute { get; init; }
    public TimeSpan TimeToResetSecond { get; init; }
    public TimeSpan TimeToResetMinute { get; init; }
}
