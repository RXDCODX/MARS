namespace MARS.WaifuGacha.Services;

public interface IShikimoriRateLimiter
{
    Task<bool> TryAcquireAsync();
    Task WaitForSlotAsync(CancellationToken cancellationToken = default);
    RateLimiterInfo GetInfo();
}
