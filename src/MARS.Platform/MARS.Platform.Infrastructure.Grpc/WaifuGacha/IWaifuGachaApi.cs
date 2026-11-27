using MARS.Shared.Contracts.WaifuGacha.Control;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using ContractClients = MARS.Shared.Contracts.Clients;
using ContractModels = MARS.Shared.Contracts.Models;
using DomainWaifu = MARS.WaifuGacha.Domain.Entities;

namespace MARS.Platform.Infrastructure.Grpc.WaifuGacha;

public interface IWaifuGachaApi
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

    Task<ContractClients.CollectionInventory?> GetFumoInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    Task<ContractClients.CollectionInventory?> GetMikuInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );

    Task<ContractClients.ShikimoriTitleRef?> GetRandomAnimeAsync(
        CancellationToken cancellationToken = default
    );

    Task<ContractClients.ShikimoriTitleRef?> GetRandomMangaAsync(
        CancellationToken cancellationToken = default
    );

    Task<List<ContractModels.MikuTrackDto>?> GetMikuMondayTracksAsync(
        CancellationToken cancellationToken = default
    );

    Task<ContractClients.MikuMondayResult?> PickMikuMondayTrackAsync(
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

    Task<List<DomainWaifu.Husband>?> GetPrivateHusbandsAsync(
        CancellationToken cancellationToken = default
    );

    Task<bool> HasFumoUserAsync(string twitchUserId, CancellationToken cancellationToken = default);

    Task<string?> UnmergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    );
}
