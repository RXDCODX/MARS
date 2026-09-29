using MARS.Admin.Entities;

namespace MARS.Admin.Services;

/// <summary>
/// Интерфейс для мониторинга состояния рейт лимитера Shikimori API
/// </summary>
public interface IShikimoriRateLimiterService
{
    RateLimiterInfo GetRateLimiterInfo();
}
