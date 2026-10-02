namespace MARS.Shared.Clients;

/// <summary>
/// Доступ к Shikimori. Владелец клиента и рейт-лимитера —
/// <c>MARS.Shikimori</c>; остальные сервисы ходят к нему по внутреннему API.
/// </summary>
public interface IShikimoriApiClient
{
    /// <summary>
    /// Случайное аниме с оценкой не ниже 7. null — сервис недоступен или
    /// Shikimori ничего не вернул.
    /// </summary>
    Task<ShikimoriTitleRef?> GetRandomAnimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Случайная манга с оценкой не ниже 7.
    /// </summary>
    Task<ShikimoriTitleRef?> GetRandomMangaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Персонаж по id Shikimori. null — сервис недоступен, персонажа нет или
    /// id неположительный.
    /// </summary>
    Task<ShikimoriCharacterRef?> GetCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Состояние рейт-лимитера. null — сервис недоступен.
    /// </summary>
    Task<ShikimoriRateLimiterInfo?> GetRateLimiterInfoAsync(
        CancellationToken cancellationToken = default
    );
}
