using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.Shared.Contracts.WaifuGacha.Rest;

/// <summary>
/// Внутренний клиент MARS.WaifuGacha — владельца супругов, вайфу и auto-hello.
/// Закрывает межсервисные зависимости MARS.TwitchCore, которым раньше не хватало
/// реализаций (<c>IAutoHelloService</c>, <c>IWaifuLookupService</c>).
/// </summary>
public interface IWaifuGachaClient
{
    Task<string?> GetWaifuNameForUserAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    Task<string?> GetAutoHelloMessageAsync(
        string twitchUserId,
        string displayName,
        CancellationToken cancellationToken = default
    );

    Task<bool> ToggleAutoHelloAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    Task<CollectionInventory?> GetFumoInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    Task<CollectionInventory?> GetMikuInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    Task<ShikimoriTitleRef?> GetRandomAnimeAsync(CancellationToken cancellationToken = default);
    Task<ShikimoriTitleRef?> GetRandomMangaAsync(CancellationToken cancellationToken = default);

    Task<List<MikuTrackDto>?> GetMikuMondayTracksAsync(
        CancellationToken cancellationToken = default
    );

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
    Task<FrogRollResultDto?> RollFrogFullAsync(
        string? twitchUserId = null,
        CancellationToken cancellationToken = default
    );
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

    Task<List<Husband>?> GetPrivateHusbandsAsync(CancellationToken cancellationToken = default);

    Task<bool> HasFumoUserAsync(string twitchUserId, CancellationToken cancellationToken = default);
    Task<string?> UnmergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );
}
