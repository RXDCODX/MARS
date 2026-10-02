namespace MARS.Shared.Clients;

/// <summary>
/// Внутренний клиент MARS.WaifuGacha — владельца супругов, вайфу и auto-hello.
/// Закрывает межсервисные зависимости MARS.TwitchCore, которым раньше не хватало
/// реализаций (<c>IAutoHelloService</c>, <c>IWaifuLookupService</c>).
/// </summary>
public interface IWaifuGachaClient
{
    /// <summary>
    /// Имя супруга пользователя для мини-игры «Русская рулетка».
    /// <c>null</c>, если пользователь не женат или сервис недоступен.
    /// </summary>
    Task<string?> GetWaifuNameForUserAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Текст авто-приветствия от супруга либо <c>null</c>, если приветствие
    /// не положено (не женат, выключено, кулдаун 20 часов не истёк).
    /// </summary>
    Task<string?> GetAutoHelloMessageAsync(
        string twitchUserId,
        string displayName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Переключает авто-приветствие пользователя и возвращает новое состояние.
    /// </summary>
    Task<bool> ToggleAutoHelloAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Инвентарь собранных фумо. null — сервис недоступен или ответил ошибкой.
    /// </summary>
    Task<CollectionInventory?> GetFumoInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Инвентарь собранных мику-модулей. null — сервис недоступен или ответил
    /// ошибкой.
    /// </summary>
    Task<CollectionInventory?> GetMikuInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Случайное аниме с Shikimori вместе с готовой ссылкой. null — сервис
    /// недоступен или источник ничего не вернул.
    /// </summary>
    Task<ShikimoriTitleRef?> GetRandomAnimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Случайная манга с Shikimori вместе с готовой ссылкой.
    /// </summary>
    Task<ShikimoriTitleRef?> GetRandomMangaAsync(CancellationToken cancellationToken = default);
}
