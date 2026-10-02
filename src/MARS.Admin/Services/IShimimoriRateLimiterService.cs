using MARS.Shared.Clients;

namespace MARS.Admin.Services;

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

/// <inheritdoc cref="IShikimoriRateLimiterService" />
public sealed class ShikimoriRateLimiterService : IShikimoriRateLimiterService
{
    private readonly IShikimoriApiClient _shikimoriClient;

    public ShikimoriRateLimiterService(IShikimoriApiClient shikimoriClient)
    {
        _shikimoriClient = shikimoriClient;
    }

    public async Task<ShikimoriRateLimiterInfo?> GetRateLimiterInfoAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _shikimoriClient.GetRateLimiterInfoAsync(cancellationToken);
    }
}
