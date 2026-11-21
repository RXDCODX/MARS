using MARS.Shared.Contracts.Clients;

namespace MARS.Platform.Infrastructure.Http;

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

    /// <summary>
    /// Доступные треки Miku Monday для текущей недели.
    /// </summary>
    Task<List<MikuTrackDto>?> GetMikuMondayTracksAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Выбор трека Miku Monday. Для стримера — без списания; для пользователя —
    /// с записью недельной активации. null — сервис недоступен.
    /// </summary>
    Task<MikuMondayResult?> PickMikuMondayTrackAsync(
        string twitchUserId,
        string displayName,
        bool isStreamer,
        CancellationToken cancellationToken = default
    );

    Task<string?> RollWaifuAsync(string hostName, CancellationToken cancellationToken = default);
    Task<string?> RollMikuAsync(CancellationToken cancellationToken = default);
    Task<string?> RollFumoAsync(CancellationToken cancellationToken = default);
    Task<string?> RollFrogAsync(CancellationToken cancellationToken = default);

    Task<WaifuRollResultDto?> RollWaifuFullAsync(
        string hostName,
        CancellationToken cancellationToken = default
    );
    Task<FumoRollResultDto?> RollFumoFullAsync(
        string? twitchUserId,
        CancellationToken cancellationToken = default
    );
    Task<MikuRollResultDto?> RollMikuFullAsync(
        string? twitchUserId,
        CancellationToken cancellationToken = default
    );
    Task<FrogRollResultDto?> RollFrogFullAsync(CancellationToken cancellationToken = default);
    Task<MergeWaifuResultDto?> MergeWaifuFullAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    Task<AddNewWaifuResultDto?> AddNewWaifuFullAsync(
        string userInput,
        string userId,
        string displayName,
        bool isVip,
        CancellationToken cancellationToken = default
    );
    Task<string?> MergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );
    Task<string?> UnmergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );
}
