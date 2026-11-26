using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.WaifuGacha.Rest;
using ContractClients = MARS.Shared.Contracts.Clients;
using ContractModels = MARS.Shared.Contracts.Models;
using DomainWaifu = MARS.WaifuGacha.Domain.Entities;
using ProtoWaifu = MARS.Shared.Contracts.WaifuGacha.Control;
using WaifuRest = MARS.Shared.Contracts.WaifuGacha.Rest;

namespace MARS.Platform.Infrastructure.Grpc.WaifuGacha;

internal sealed class WaifuGachaApi(GrpcClientFactory clientFactory) : IWaifuGachaApi
{
    private const string ServiceName = "WaifuGacha";

    public async Task<string?> GetWaifuNameForUserAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .GetWaifuNameAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.GetWaifuNameRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    public async Task<string?> GetAutoHelloMessageAsync(
        string twitchUserId,
        string displayName,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .GetAutoHelloAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.GetAutoHelloRequest
                {
                    TwitchUserId = twitchUserId,
                    DisplayName = displayName,
                },
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    public async Task<bool> ToggleAutoHelloAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .ToggleAutoHelloAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.ToggleAutoHelloRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        return response.Value;
    }

    public async Task<ContractClients.CollectionInventory?> GetFumoInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .GetFumoInventoryAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.InventoryRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        return ToModel(response);
    }

    public async Task<ContractClients.CollectionInventory?> GetMikuInventoryAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .GetMikuInventoryAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.InventoryRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        return ToModel(response);
    }

    public async Task<ContractClients.ShikimoriTitleRef?> GetRandomAnimeAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await Client()
                .GetRandomAnimeAsync(
                    new global::MARS.Shared.Contracts.WaifuGacha.Control.EmptyRequest(),
                    cancellationToken: cancellationToken
                );
            return ToModel(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ContractClients.ShikimoriTitleRef?> GetRandomMangaAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await Client()
                .GetRandomMangaAsync(
                    new global::MARS.Shared.Contracts.WaifuGacha.Control.EmptyRequest(),
                    cancellationToken: cancellationToken
                );
            return ToModel(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<ContractModels.MikuTrackDto>?> GetMikuMondayTracksAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .GetMikuMondayTracksAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.EmptyRequest(),
                cancellationToken: cancellationToken
            );
        return response.Tracks.Select(ToModel).ToList();
    }

    public async Task<ContractClients.MikuMondayResult?> PickMikuMondayTrackAsync(
        string twitchUserId,
        string displayName,
        bool isStreamer,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .PickMikuMondayTrackAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.PickMikuMondayTrackRequest
                {
                    TwitchUserId = twitchUserId,
                    DisplayName = displayName,
                    IsStreamer = isStreamer,
                },
                cancellationToken: cancellationToken
            );

        return new ContractClients.MikuMondayResult
        {
            Track = response.Track is null ? null : ToModel(response.Track),
            AvailableTracks = response.AvailableTracks.Select(ToModel).ToList(),
            Error = string.IsNullOrWhiteSpace(response.Error) ? null : response.Error,
        };
    }

    public async Task<string?> RollWaifuAsync(
        string hostName,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .RollWaifuAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.RollWaifuRequest
                {
                    HostName = hostName,
                },
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    public async Task<string?> RollMikuAsync(CancellationToken cancellationToken = default)
    {
        var response = await Client()
            .RollMikuAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.EmptyRequest(),
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    public async Task<string?> RollFumoAsync(CancellationToken cancellationToken = default)
    {
        var response = await Client()
            .RollFumoAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.EmptyRequest(),
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    public async Task<string?> RollFrogAsync(CancellationToken cancellationToken = default)
    {
        var response = await Client()
            .RollFrogAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.EmptyRequest(),
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    public async Task<WaifuRest.WaifuRollResultDto?> RollWaifuFullAsync(
        string hostName,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .RollWaifuFullAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.RollWaifuFullRequest
                {
                    HostName = hostName,
                },
                cancellationToken: cancellationToken
            );
        if (response.Waifu is null && response.Host is null && !response.IsCooldown)
        {
            return null;
        }

        return new WaifuRest.WaifuRollResultDto(
            ToModel(response.Waifu),
            ToModel(response.Host),
            response.IsCooldown,
            response.RemainingSeconds
        );
    }

    public async Task<WaifuRest.FumoRollResultDto?> RollFumoFullAsync(
        string? twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .RollFumoFullAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.RollFumoFullRequest
                {
                    TwitchUserId = twitchUserId ?? string.Empty,
                },
                cancellationToken: cancellationToken
            );
        if (response.Fumo is null && !response.IsCooldown)
        {
            return null;
        }

        return new WaifuRest.FumoRollResultDto(
            ToModel(response.Fumo),
            response.CollectedCount,
            response.TotalCount,
            response.IsCooldown,
            response.RemainingSeconds
        );
    }

    public async Task<WaifuRest.MikuRollResultDto?> RollMikuFullAsync(
        string? twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .RollMikuFullAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.RollMikuFullRequest
                {
                    TwitchUserId = twitchUserId ?? string.Empty,
                },
                cancellationToken: cancellationToken
            );
        if (response.Module is null && !response.IsCooldown)
        {
            return null;
        }

        return new WaifuRest.MikuRollResultDto(
            ToModel(response.Module),
            response.CollectedCount,
            response.TotalCount,
            response.IsCooldown,
            response.RemainingSeconds
        );
    }

    public async Task<WaifuRest.FrogRollResultDto?> RollFrogFullAsync(
        string? twitchUserId = null,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .RollFrogFullAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.RollFrogFullRequest
                {
                    TwitchUserId = twitchUserId ?? string.Empty,
                },
                cancellationToken: cancellationToken
            );
        if (response.Frog is null && !response.IsCooldown)
        {
            return null;
        }

        return new WaifuRest.FrogRollResultDto(
            ToModel(response.Frog),
            response.IsCooldown,
            response.RemainingSeconds
        );
    }

    public async Task<WaifuRest.MergeWaifuResultDto?> MergeWaifuFullAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .MergeHusbandFullAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.MergeHusbandFullRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        if (response.Waifu is null && response.Host is null)
        {
            return null;
        }

        return new WaifuRest.MergeWaifuResultDto(
            ToModel(response.Waifu),
            ToModel(response.Host),
            response.IsNewMarriage
        );
    }

    public async Task<WaifuRest.AddNewWaifuResultDto?> AddNewWaifuFullAsync(
        string userInput,
        string userId,
        string displayName,
        bool isVip,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .AddNewWaifuFullAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.AddNewWaifuFullRequest
                {
                    UserInput = userInput,
                    UserId = userId,
                    DisplayName = displayName,
                    IsVip = isVip,
                },
                cancellationToken: cancellationToken
            );

        if (response.Waifu is null)
        {
            return null;
        }

        return new WaifuRest.AddNewWaifuResultDto(
            ToModel(response.Waifu),
            response.DisplayName,
            response.Color,
            response.VipDropped,
            response.RollsUntilGuarantee
        );
    }

    public async Task<string?> MergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .MergeHusbandAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.MergeHusbandRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    public async Task<bool> HasFumoUserAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .HasFumoUserAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.HasFumoUserRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        return response.Value;
    }

    public async Task<List<DomainWaifu.Husband>?> GetPrivateHusbandsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .GetPrivateHusbandsAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.EmptyRequest(),
                cancellationToken: cancellationToken
            );
        return response
            .Husbands.Select(ToModel)
            .Where(item => item is not null)
            .Cast<DomainWaifu.Husband>()
            .ToList();
    }

    private global::MARS.Shared.Contracts.WaifuGacha.Control.WaifuGachaService.WaifuGachaServiceClient Client() =>
        clientFactory.CreateClient<global::MARS.Shared.Contracts.WaifuGacha.Control.WaifuGachaService.WaifuGachaServiceClient>(
            ServiceName
        );

    public async Task<string?> UnmergeHusbandAsync(
        string twitchUserId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Client()
            .UnmergeHusbandAsync(
                new global::MARS.Shared.Contracts.WaifuGacha.Control.UnmergeHusbandRequest
                {
                    TwitchUserId = twitchUserId,
                },
                cancellationToken: cancellationToken
            );
        return response.HasValue ? response.Value : null;
    }

    private static ContractClients.CollectionInventory ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.CollectionInventory proto
    ) =>
        new(
            proto.Collected,
            proto.Total,
            proto
                .Items.Select(item => new ContractClients.CollectionItem(item.Name, item.Count))
                .ToList()
        );

    private static ContractModels.MikuTrackDto ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.MikuTrackDto proto
    ) =>
        new()
        {
            Id = Guid.Parse(proto.Id),
            Number = proto.Number,
            Artist = proto.Artist,
            Title = proto.Title,
            Url = proto.Url,
            ThumbnailUrl = string.IsNullOrWhiteSpace(proto.ThumbnailUrl)
                ? null
                : proto.ThumbnailUrl,
        };

    private static ContractClients.MikuMondayResult ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.MikuMondayResult proto
    ) =>
        new()
        {
            Track = proto.Track is null ? null : ToModel(proto.Track),
            AvailableTracks = proto.AvailableTracks.Select(ToModel).ToList(),
            Error = string.IsNullOrWhiteSpace(proto.Error) ? null : proto.Error,
        };

    private static DomainWaifu.Husband? ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.Husband? proto
    ) =>
        proto is null
            ? null
            : new DomainWaifu.Husband
            {
                TwitchId = proto.TwitchId,
                WhenOrdered = proto.WhenOrdered.ToDateTime(),
                WaifuBrideId = string.IsNullOrWhiteSpace(proto.WaifuBrideId)
                    ? null
                    : proto.WaifuBrideId,
                IsPrivated = proto.IsPrivated,
                OrderCount = proto.OrderCount,
                WaifuRollId = string.IsNullOrWhiteSpace(proto.WaifuRollId)
                    ? null
                    : proto.WaifuRollId,
                WhenPrivated = proto.WhenPrivated is null ? null : proto.WhenPrivated.ToDateTime(),
                HusbandGreetings = proto.HusbandGreetings is null
                    ? null
                    : new DomainWaifu.HusbandAutoHello
                    {
                        Guid = Guid.Parse(proto.HusbandGreetings.Guid),
                        HusbandId = proto.HusbandGreetings.HusbandId,
                        Time = proto.HusbandGreetings.Time.ToDateTime(),
                    },
                HusbandCoolDown = proto.HusbandCoolDown is null
                    ? null
                    : new DomainWaifu.HusbandCoolDown
                    {
                        Guid = Guid.Parse(proto.HusbandCoolDown.Guid),
                        HusbandId = proto.HusbandCoolDown.HusbandId,
                        Time = proto.HusbandCoolDown.Time.ToDateTime(),
                    },
                LastWeddingCongratulatedMonths = proto.HasLastWeddingCongratulatedMonths
                    ? proto.LastWeddingCongratulatedMonths
                    : null,
                IsAutoHelloEnabled = proto.IsAutoHelloEnabled,
                TwitchUser = proto.TwitchUser is null
                    ? null
                    : new MARS.Shared.Kernel.TwitchUser
                    {
                        TwitchId = proto.TwitchUser.TwitchId,
                        UserLogin = proto.TwitchUser.UserLogin,
                        DisplayName = proto.TwitchUser.DisplayName,
                        ProfileImageUrl = proto.TwitchUser.ProfileImageUrl,
                        ChatColor = proto.TwitchUser.ChatColor,
                        IsModerator = proto.TwitchUser.IsModerator,
                        IsVip = proto.TwitchUser.IsVip,
                    },
            };

    private static DomainWaifu.Waifu? ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.Waifu? proto
    ) =>
        proto is null
            ? null
            : new DomainWaifu.Waifu
            {
                ShikiId = proto.ShikiId,
                Name = proto.Name,
                Age = proto.Age,
                Anime = string.IsNullOrWhiteSpace(proto.Anime) ? null : proto.Anime,
                Manga = string.IsNullOrWhiteSpace(proto.Manga) ? null : proto.Manga,
                WhenAdded = proto.WhenAdded.ToDateTime(),
                LastOrder = proto.LastOrder.ToDateTime(),
                OrderCount = proto.OrderCount,
                IsPrivated = proto.IsPrivated,
                ImageUrl = proto.ImageUrl,
                AudioId = null,
                IsMerged = proto.IsMerged,
                IsAdded = proto.IsAdded,
            };

    private static DomainWaifu.Fumo? ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.Fumo? proto
    ) =>
        proto is null
            ? null
            : new DomainWaifu.Fumo
            {
                MfcId = proto.MfcId,
                Name = proto.Name,
                Character = proto.Character,
                CharacterTranslit = string.IsNullOrWhiteSpace(proto.CharacterTranslit)
                    ? null
                    : proto.CharacterTranslit,
                Origin = string.IsNullOrWhiteSpace(proto.Origin) ? null : proto.Origin,
                ThumbnailUrl = proto.ThumbnailUrl,
                Rating = proto.Rating,
                RatingCount = proto.RatingCount,
                WhenAdded = proto.WhenAdded.ToDateTime(),
                LastOrder = proto.LastOrder.ToDateTime(),
                OrderCount = proto.OrderCount,
            };

    private static DomainWaifu.MikuModule? ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.MikuModule? proto
    ) =>
        proto is null
            ? null
            : new DomainWaifu.MikuModule
            {
                PageId = proto.PageId,
                Title = proto.Title,
                JapaneseName = string.IsNullOrWhiteSpace(proto.JapaneseName)
                    ? null
                    : proto.JapaneseName,
                Designer = string.IsNullOrWhiteSpace(proto.Designer) ? null : proto.Designer,
                ThumbnailUrl = proto.ThumbnailUrl,
                Description = string.IsNullOrWhiteSpace(proto.Description)
                    ? null
                    : proto.Description,
                Songs = string.IsNullOrWhiteSpace(proto.Songs) ? null : proto.Songs,
                WhenAdded = proto.WhenAdded.ToDateTime(),
                LastOrder = proto.LastOrder.ToDateTime(),
                OrderCount = proto.OrderCount,
            };

    private static DomainWaifu.Frog? ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.Frog? proto
    ) =>
        proto is null
            ? null
            : new DomainWaifu.Frog
            {
                Pid = proto.Pid,
                CommonName = proto.CommonName,
                ScientificName = proto.ScientificName,
                Family = string.IsNullOrWhiteSpace(proto.Family) ? null : proto.Family,
                RussianName = string.IsNullOrWhiteSpace(proto.RussianName)
                    ? null
                    : proto.RussianName,
                ThumbnailUrl = proto.ThumbnailUrl,
                Size = string.IsNullOrWhiteSpace(proto.Size) ? null : proto.Size,
                Status = string.IsNullOrWhiteSpace(proto.Status) ? null : proto.Status,
                Category = string.IsNullOrWhiteSpace(proto.Category) ? null : proto.Category,
                Habits = string.IsNullOrWhiteSpace(proto.Habits) ? null : proto.Habits,
                WhenAdded = proto.WhenAdded.ToDateTime(),
                LastOrder = proto.LastOrder.ToDateTime(),
                OrderCount = proto.OrderCount,
            };

    private static ContractClients.ShikimoriTitleRef ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.ShikimoriTitleRef proto
    ) =>
        new(
            proto.Id,
            string.IsNullOrWhiteSpace(proto.Name) ? null : proto.Name,
            string.IsNullOrWhiteSpace(proto.RussianName) ? null : proto.RussianName,
            proto.HasYear ? proto.Year : null,
            proto.Url
        );

    private static MARS.Shared.Contracts.WaifuGacha.Rest.WaifuRollResultDto ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.WaifuRollResult proto
    ) => new(ToModel(proto.Waifu), ToModel(proto.Host), proto.IsCooldown, proto.RemainingSeconds);

    private static WaifuRest.FumoRollResultDto ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.FumoRollResult proto
    ) =>
        new(
            ToModel(proto.Fumo),
            proto.CollectedCount,
            proto.TotalCount,
            proto.IsCooldown,
            proto.RemainingSeconds
        );

    private static WaifuRest.MikuRollResultDto ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.MikuRollResult proto
    ) =>
        new(
            ToModel(proto.Module),
            proto.CollectedCount,
            proto.TotalCount,
            proto.IsCooldown,
            proto.RemainingSeconds
        );

    private static WaifuRest.FrogRollResultDto ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.FrogRollResult proto
    ) => new(ToModel(proto.Frog)!, proto.IsCooldown, proto.RemainingSeconds);

    private static WaifuRest.MergeWaifuResultDto ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.MergeWaifuResult proto
    ) => new(ToModel(proto.Waifu), ToModel(proto.Host), proto.IsNewMarriage);

    private static WaifuRest.AddNewWaifuResultDto ToModel(
        global::MARS.Shared.Contracts.WaifuGacha.Control.AddNewWaifuResult proto
    ) =>
        new(
            ToModel(proto.Waifu)!,
            proto.DisplayName,
            proto.Color,
            proto.VipDropped,
            proto.RollsUntilGuarantee
        );
}
