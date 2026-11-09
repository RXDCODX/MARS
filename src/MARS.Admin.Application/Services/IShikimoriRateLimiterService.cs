using MARS.Shared.Contracts.Clients;

namespace MARS.Admin.Application.Services;

/// <summary>
/// Мониторинг рейт-лимитера Shikimori API.
/// </summary>
/// <remarks>
/// Данные берутся у <c>MARS.Shikimori</c> — он владеет клиентом и лимитером.
/// До выделения сервиса интерфейс оставался в MARS.Admin без реализации и без
/// регистрации: контроллер <c>/api/ShikimoriRateLimiter/info</c> отдавал 500 при
/// попытке собрать зависимость.
/// </remarks>
public interface IShikimoriRateLimiterService
{
    Task<ShikimoriRateLimiterInfo?> GetRateLimiterInfoAsync(
        CancellationToken cancellationToken = default
    );
}
