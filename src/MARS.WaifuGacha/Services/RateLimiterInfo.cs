namespace MARS.WaifuGacha.Services;

public record RateLimiterInfo
{
    public int AvailablePerSecond { get; init; }
    public int AvailablePerMinute { get; init; }
    public TimeSpan TimeToResetSecond { get; init; }
    public TimeSpan TimeToResetMinute { get; init; }
}
