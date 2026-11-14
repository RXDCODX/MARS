using MARS.Shikimori.Application.Models;

namespace MARS.Shikimori.Application.Ports;

public interface IShikimoriRateLimiter
{
    Task<bool> TryAcquireAsync();
    Task WaitForSlotAsync(CancellationToken cancellationToken = default);
    RateLimiterInfo GetInfo();
}
