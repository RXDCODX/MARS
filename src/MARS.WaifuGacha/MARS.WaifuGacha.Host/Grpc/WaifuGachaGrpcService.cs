using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.WaifuGacha.Control;
using MARS.WaifuGacha.Application.Models;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;
using MARS.WaifuGacha.Infrastructure.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainFrog = MARS.WaifuGacha.Domain.Entities.Frog;
using DomainFumo = MARS.WaifuGacha.Domain.Entities.Fumo;
using DomainHusband = MARS.WaifuGacha.Domain.Entities.Husband;
using DomainMikuModule = MARS.WaifuGacha.Domain.Entities.MikuModule;
using DomainWaifu = MARS.WaifuGacha.Domain.Entities.Waifu;
using ProtoAddNewWaifuResult = MARS.Shared.Contracts.WaifuGacha.Control.AddNewWaifuResult;
using ProtoCollectionInventory = MARS.Shared.Contracts.WaifuGacha.Control.CollectionInventory;
using ProtoCollectionItem = MARS.Shared.Contracts.WaifuGacha.Control.CollectionItem;
using ProtoFrog = MARS.Shared.Contracts.WaifuGacha.Control.Frog;
using ProtoFumo = MARS.Shared.Contracts.WaifuGacha.Control.Fumo;
using ProtoHusband = MARS.Shared.Contracts.WaifuGacha.Control.Husband;
using ProtoHusbandAutoHello = MARS.Shared.Contracts.WaifuGacha.Control.HusbandAutoHello;
using ProtoHusbandCoolDown = MARS.Shared.Contracts.WaifuGacha.Control.HusbandCoolDown;
using ProtoMergeWaifuResult = MARS.Shared.Contracts.WaifuGacha.Control.MergeWaifuResult;
using ProtoMikuModule = MARS.Shared.Contracts.WaifuGacha.Control.MikuModule;
using ProtoMikuMondayResult = MARS.Shared.Contracts.WaifuGacha.Control.MikuMondayResult;
using ProtoMikuTrackDto = MARS.Shared.Contracts.WaifuGacha.Control.MikuTrackDto;
using ProtoShikimoriTitleRef = MARS.Shared.Contracts.WaifuGacha.Control.ShikimoriTitleRef;
using ProtoTwitchUser = MARS.Shared.Contracts.WaifuGacha.Control.TwitchUser;
using ProtoWaifu = MARS.Shared.Contracts.WaifuGacha.Control.Waifu;

namespace MARS.WaifuGacha.Host.Grpc;

public sealed class WaifuGachaGrpcService(
    AutoHelloService autoHelloService,
    FumoCollectionService fumoCollectionService,
    MikuCollectionService mikuCollectionService,
    MikuMondayService mikuMondayService,
    WaifuRollService waifuRollService,
    MikuRollService mikuRollService,
    FumoRollService fumoRollService,
    FrogRollService frogRollService,
    RollCooldownService cooldownService,
    RollCooldownConfigurationService cooldownConfiguration,
    RollCooldownNotificationState notificationState,
    MergeWaifuService mergeWaifuService,
    AddNewWaifuService addNewWaifuService,
    IMarsGrpcClient grpcClient,
    IDbContextFactory<WaifuDbContext> dbFactory,
    ILogger<WaifuGachaGrpcService> logger
) : WaifuGachaService.WaifuGachaServiceBase
{
    public override async Task<GetPrivateHusbandsResponse> GetPrivateHusbands(
        EmptyRequest request,
        ServerCallContext context
    )
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        var husbands = await db
            .Husbands.AsNoTracking()
            .Where(h => h.IsPrivated && h.WhenPrivated != null)
            .ToListAsync(context.CancellationToken);

        var response = new GetPrivateHusbandsResponse();
        response.Husbands.AddRange(husbands.Select(ToProto));
        return response;
    }

    public override async Task<BoolResult> HasFumoUser(
        HasFumoUserRequest request,
        ServerCallContext context
    )
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        var has = await db.FumoRollUsers.AnyAsync(
            u => u.TwitchId == request.TwitchUserId,
            context.CancellationToken
        );
        return new BoolResult { Value = has };
    }

    public override async Task<StringResult> GetWaifuName(
        GetWaifuNameRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.TwitchUserId))
        {
            return new StringResult { HasValue = false };
        }

        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        var name = await db
            .Husbands.AsNoTracking()
            .Where(h => h.TwitchId == request.TwitchUserId && h.IsPrivated)
            .Where(h => h.WaifuBrideId != null)
            .Join(db.Waifus.AsNoTracking(), h => h.WaifuBrideId, w => w.ShikiId, (_, w) => w.Name)
            .FirstOrDefaultAsync(context.CancellationToken);

        return new StringResult
        {
            Value = string.IsNullOrWhiteSpace(name) ? string.Empty : name,
            HasValue = !string.IsNullOrWhiteSpace(name),
        };
    }

    public override async Task<StringResult> GetAutoHello(
        GetAutoHelloRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.TwitchUserId) || request is null)
        {
            return new StringResult { HasValue = false };
        }

        var message = await autoHelloService.GetAutoHelloMessageAsync(
            request.TwitchUserId,
            request.DisplayName,
            context.CancellationToken
        );
        return new StringResult
        {
            Value = message ?? string.Empty,
            HasValue = !string.IsNullOrWhiteSpace(message),
        };
    }

    public override async Task<BoolResult> ToggleAutoHello(
        ToggleAutoHelloRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.TwitchUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Twitch ID не передан"));
        }

        var enabled = await autoHelloService.ToggleAutoHelloEnabledAsync(
            request.TwitchUserId,
            context.CancellationToken
        );
        return new BoolResult { Value = enabled };
    }

    public override async Task<ProtoCollectionInventory> GetFumoInventory(
        InventoryRequest request,
        ServerCallContext context
    )
    {
        var result = await fumoCollectionService.GetInventoryAsync(
            request.TwitchUserId,
            context.CancellationToken
        );
        if (!result.Success || result.Result is null)
        {
            return new ProtoCollectionInventory();
        }

        return ToProto(result.Result);
    }

    public override async Task<ProtoCollectionInventory> GetMikuInventory(
        InventoryRequest request,
        ServerCallContext context
    )
    {
        var result = await mikuCollectionService.GetInventoryAsync(
            request.TwitchUserId,
            context.CancellationToken
        );
        if (!result.Success || result.Result is null)
        {
            return new ProtoCollectionInventory();
        }

        return ToProto(result.Result);
    }

    public override async Task<ProtoShikimoriTitleRef> GetRandomAnime(
        EmptyRequest request,
        ServerCallContext context
    )
    {
        var anime = await grpcClient.Shikimori.GetRandomAnimeAsync(context.CancellationToken);
        if (anime is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Shikimori не вернул аниме"));
        }

        return ToProto(anime);
    }

    public override async Task<ProtoShikimoriTitleRef> GetRandomManga(
        EmptyRequest request,
        ServerCallContext context
    )
    {
        var manga = await grpcClient.Shikimori.GetRandomMangaAsync(context.CancellationToken);
        if (manga is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Shikimori не вернул мангу"));
        }

        return ToProto(manga);
    }

    public override async Task<MikuMondayTracksResponse> GetMikuMondayTracks(
        EmptyRequest request,
        ServerCallContext context
    )
    {
        var tracks = await mikuMondayService.GetAvailableTracksAsync(context.CancellationToken);
        var response = new MikuMondayTracksResponse();
        response.Tracks.AddRange(tracks.Select(ToProto));
        return response;
    }

    public override async Task<ProtoMikuMondayResult> PickMikuMondayTrack(
        PickMikuMondayTrackRequest request,
        ServerCallContext context
    )
    {
        MARS.Shared.Contracts.Clients.MikuMondayResult result;
        if (request.IsStreamer)
        {
            result = await mikuMondayService.GetRandomTrackForStreamerAsync(
                context.CancellationToken
            );
        }
        else
        {
            result = await mikuMondayService.GetRandomTrackForUserAsync(
                request.TwitchUserId,
                request.DisplayName,
                context.CancellationToken
            );
        }

        return new ProtoMikuMondayResult
        {
            Track = result.Track is null ? null : ToProto(result.Track),
            AvailableTracks = { result.AvailableTracks.Select(ToProto) },
            Error = result.Error ?? string.Empty,
        };
    }

    public override async Task<StringResult> RollWaifu(
        RollWaifuRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.HostName))
        {
            return new StringResult { HasValue = false };
        }

        var result = await waifuRollService.TelegramRollWaifu(request.HostName);
        if (!result.Success || result.Result?.Waifu is null)
        {
            return new StringResult { HasValue = false };
        }

        var hostName = result.Result.Host?.TwitchId ?? request.HostName;
        return new StringResult { Value = $"Вайфу-ролл для {hostName} выполнен", HasValue = true };
    }

    public override async Task<StringResult> RollMiku(
        EmptyRequest request,
        ServerCallContext context
    )
    {
        var module = await mikuRollService.RollTheMiku();
        return module is null
            ? new StringResult { HasValue = false }
            : new StringResult { Value = $"Miku roll: {module.Title}", HasValue = true };
    }

    public override async Task<StringResult> RollFumo(
        EmptyRequest request,
        ServerCallContext context
    )
    {
        var fumo = await fumoRollService.RollTheFumo();
        return fumo is null
            ? new StringResult { HasValue = false }
            : new StringResult { Value = $"Fumo roll: {fumo.Name}", HasValue = true };
    }

    public override async Task<StringResult> RollFrog(
        EmptyRequest request,
        ServerCallContext context
    )
    {
        var frog = await frogRollService.RollTheFrog();
        return frog is null
            ? new StringResult { HasValue = false }
            : new StringResult { Value = $"Frog roll: {frog.CommonName}", HasValue = true };
    }

    public override async Task<StringResult> MergeHusband(
        MergeHusbandRequest request,
        ServerCallContext context
    )
    {
        var result = await mergeWaifuService.MergeWaifuAsync(request.TwitchUserId);
        return new StringResult
        {
            Value = result.Success
                ? (result.ErrorMessage ?? "Объединение выполнено")
                : (result.ErrorMessage ?? "Не удалось объединить"),
            HasValue = result.Success,
        };
    }

    public override async Task<FrogRollResult> RollFrogFull(
        RollFrogFullRequest request,
        ServerCallContext context
    )
    {
        var cooldown = await cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.FrogRollType,
            context.CancellationToken
        );
        var (allowed, remaining) = await cooldownService.CheckAndUpdateCooldownAsync(
            request.TwitchUserId ?? string.Empty,
            RootStateKeys.FrogRollType,
            cooldown,
            context.CancellationToken
        );
        if (!allowed)
        {
            return new FrogRollResult
            {
                IsCooldown = true,
                RemainingSeconds = (int)remaining.TotalSeconds,
            };
        }

        await notificationState.AddPendingAsync(
            request.TwitchUserId ?? string.Empty,
            RootStateKeys.FrogRollType,
            cooldown,
            context.CancellationToken
        );
        var frog = await frogRollService.RollTheFrog();
        if (frog is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Не удалось найти лягушку"));
        }

        return new FrogRollResult
        {
            Frog = ToProto(frog),
            IsCooldown = false,
            RemainingSeconds = 0,
        };
    }

    public override async Task<FumoRollResult> RollFumoFull(
        RollFumoFullRequest request,
        ServerCallContext context
    )
    {
        var cooldown = await cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.FumoRollType,
            context.CancellationToken
        );
        var (allowed, remaining) = await cooldownService.CheckAndUpdateCooldownAsync(
            request.TwitchUserId ?? string.Empty,
            RootStateKeys.FumoRollType,
            cooldown,
            context.CancellationToken
        );
        if (!allowed)
        {
            return new FumoRollResult
            {
                IsCooldown = true,
                RemainingSeconds = (int)remaining.TotalSeconds,
            };
        }

        await notificationState.AddPendingAsync(
            request.TwitchUserId ?? string.Empty,
            RootStateKeys.FumoRollType,
            cooldown,
            context.CancellationToken
        );
        var fumo = await fumoRollService.RollTheFumo();
        if (fumo is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Не удалось найти фумо"));
        }

        var stats = await fumoCollectionService.RecordRollAsync(
            request.TwitchUserId ?? string.Empty,
            fumo.MfcId
        );
        return new FumoRollResult
        {
            Fumo = ToProto(fumo),
            CollectedCount = stats.CollectedCount,
            TotalCount = stats.TotalCount,
            IsCooldown = false,
            RemainingSeconds = 0,
        };
    }

    public override async Task<MikuRollResult> RollMikuFull(
        RollMikuFullRequest request,
        ServerCallContext context
    )
    {
        var cooldown = await cooldownConfiguration.GetCooldownAsync(
            RootStateKeys.MikuRollType,
            context.CancellationToken
        );
        var (allowed, remaining) = await cooldownService.CheckAndUpdateCooldownAsync(
            request.TwitchUserId ?? string.Empty,
            RootStateKeys.MikuRollType,
            cooldown,
            context.CancellationToken
        );
        if (!allowed)
        {
            return new MikuRollResult
            {
                IsCooldown = true,
                RemainingSeconds = (int)remaining.TotalSeconds,
            };
        }

        await notificationState.AddPendingAsync(
            request.TwitchUserId ?? string.Empty,
            RootStateKeys.MikuRollType,
            cooldown,
            context.CancellationToken
        );
        var module = await mikuRollService.RollTheMiku();
        if (module is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Не удалось найти костюм Miku"));
        }

        var stats = await mikuCollectionService.RecordRollAsync(
            request.TwitchUserId ?? string.Empty,
            module.PageId
        );
        return new MikuRollResult
        {
            Module = ToProto(module),
            CollectedCount = stats.CollectedCount,
            TotalCount = stats.TotalCount,
            IsCooldown = false,
            RemainingSeconds = 0,
        };
    }

    public override async Task<WaifuRollResult> RollWaifuFull(
        RollWaifuFullRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.HostName))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Имя хоста не передано"));
        }

        var result = await waifuRollService.TelegramRollWaifu(request.HostName);
        if (!result.Success || result.Result?.Waifu is null)
        {
            var host = result.Result?.Host;
            var cooldownTime = host?.HusbandCoolDown?.Time;
            var remaining = TimeSpan.Zero;
            if (cooldownTime.HasValue && cooldownTime.Value != default)
            {
                var cooldownDuration = await waifuRollService.GetWaifuRollCoolDownAsync(
                    context.CancellationToken
                );
                var elapsed = DateTime.UtcNow - cooldownTime.Value;
                if (elapsed < cooldownDuration)
                {
                    remaining = cooldownDuration - elapsed;
                }
            }

            if (remaining > TimeSpan.Zero)
            {
                return new WaifuRollResult
                {
                    Waifu = null,
                    Host = ToProto(host),
                    IsCooldown = true,
                    RemainingSeconds = (int)remaining.TotalSeconds,
                };
            }

            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    result.ErrorMessage ?? "Не удалось выполнить вайфу-ролл"
                )
            );
        }

        var r = result.Result!;
        if (r.Host is not null && r.Host.TwitchUser is null)
        {
            var restUser = await grpcClient.Twitch.GetFullUserByLoginAsync(
                r.Host.TwitchId,
                context.CancellationToken
            );
            if (restUser is not null)
            {
                r.Host.TwitchUser = new MARS.Shared.Kernel.TwitchUser
                {
                    TwitchId = restUser.TwitchId,
                    UserLogin = restUser.UserLogin,
                    DisplayName = restUser.DisplayName,
                    ProfileImageUrl = restUser.ProfileImageUrl,
                    ChatColor = restUser.ChatColor,
                    IsModerator = restUser.IsModerator,
                    IsVip = restUser.IsVip,
                };
            }
        }

        if (r.Host is not null && !string.IsNullOrWhiteSpace(r.Host.TwitchId))
        {
            var cooldownDuration = await waifuRollService.GetWaifuRollCoolDownAsync(
                context.CancellationToken
            );
            await notificationState.AddPendingAsync(
                r.Host.TwitchId,
                RootStateKeys.WaifuRollType,
                cooldownDuration,
                context.CancellationToken
            );
        }

        return new WaifuRollResult
        {
            Waifu = ToProto(r.Waifu),
            Host = ToProto(r.Host),
            IsCooldown = false,
            RemainingSeconds = 0,
        };
    }

    public override async Task<ProtoMergeWaifuResult> MergeHusbandFull(
        MergeHusbandFullRequest request,
        ServerCallContext context
    )
    {
        var result = await mergeWaifuService.MergeWaifuAsync(request.TwitchUserId);
        if (!result.Success || result.Result is null)
        {
            throw new RpcException(
                new Status(StatusCode.Internal, result.ErrorMessage ?? "Не удалось объединить")
            );
        }

        var r = result.Result;
        if (r.Host is not null && r.Host.TwitchUser is null)
        {
            var restUser = await grpcClient.Twitch.GetFullUserByLoginAsync(
                r.Host.TwitchId,
                context.CancellationToken
            );
            if (restUser is not null)
            {
                r.Host.TwitchUser = new MARS.Shared.Kernel.TwitchUser
                {
                    TwitchId = restUser.TwitchId,
                    UserLogin = restUser.UserLogin,
                    DisplayName = restUser.DisplayName,
                    ProfileImageUrl = restUser.ProfileImageUrl,
                    ChatColor = restUser.ChatColor,
                    IsModerator = restUser.IsModerator,
                    IsVip = restUser.IsVip,
                };
            }
        }

        return new ProtoMergeWaifuResult
        {
            Waifu = ToProto(r.Waifu),
            Host = ToProto(r.Host),
            IsNewMarriage = r.IsNewMarriage,
        };
    }

    public override async Task<ProtoAddNewWaifuResult> AddNewWaifuFull(
        AddNewWaifuFullRequest request,
        ServerCallContext context
    )
    {
        var result = await addNewWaifuService.AddNewWaifuAsync(
            request.UserInput,
            request.UserId,
            request.DisplayName,
            request.IsVip
        );
        if (!result.Success || result.Result?.Waifu is null)
        {
            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    result.ErrorMessage ?? "Не удалось добавить супруга"
                )
            );
        }

        var r = result.Result;
        return new ProtoAddNewWaifuResult
        {
            Waifu = ToProto(r.Waifu),
            DisplayName = r.DisplayName ?? request.DisplayName,
            Color = "#ffffff",
            VipDropped = r.VipDropped,
            RollsUntilGuarantee = r.RollsUntilGuarantee,
        };
    }

    public override async Task<StringResult> UnmergeHusband(
        UnmergeHusbandRequest request,
        ServerCallContext context
    )
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        var host = await db.Husbands.FirstOrDefaultAsync(
            h => h.TwitchId == request.TwitchUserId,
            context.CancellationToken
        );
        if (host is not { IsPrivated: true })
        {
            return new StringResult { Value = "Муж не найден или не в браке", HasValue = false };
        }

        var waifu = await db.Waifus.FirstOrDefaultAsync(
            w => w.ShikiId == host.WaifuBrideId,
            context.CancellationToken
        );
        if (waifu is not { IsPrivated: true })
        {
            return new StringResult
            {
                Value = $"Не удалось найти вайфу этого мужа ({host.TwitchId})",
                HasValue = false,
            };
        }

        waifu.IsPrivated = false;
        host.WaifuBrideId = null;
        host.IsPrivated = false;
        host.WhenPrivated = null;

        db.Waifus.Update(waifu);
        db.Husbands.Update(host);
        await db.SaveChangesAsync(context.CancellationToken);

        return new StringResult { Value = $"Развод выполнен: {waifu.Name}", HasValue = true };
    }

    private static ProtoHusband? ToProto(DomainHusband? source) =>
        source is null
            ? null
            : new ProtoHusband
            {
                TwitchId = source.TwitchId,
                WhenOrdered = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.WhenOrdered, DateTimeKind.Utc)
                ),
                WaifuBrideId = source.WaifuBrideId ?? string.Empty,
                IsPrivated = source.IsPrivated,
                OrderCount = source.OrderCount,
                WaifuRollId = source.WaifuRollId ?? string.Empty,
                WhenPrivated = source.WhenPrivated.HasValue
                    ? Timestamp.FromDateTime(
                        DateTime.SpecifyKind(source.WhenPrivated.Value, DateTimeKind.Utc)
                    )
                    : null,
                IsAutoHelloEnabled = source.IsAutoHelloEnabled,
                HusbandGreetings = source.HusbandGreetings is null
                    ? null
                    : new ProtoHusbandAutoHello
                    {
                        Guid = source.HusbandGreetings.Guid.ToString(),
                        HusbandId = source.HusbandGreetings.HusbandId,
                        Time = Timestamp.FromDateTime(
                            DateTime.SpecifyKind(source.HusbandGreetings.Time, DateTimeKind.Utc)
                        ),
                    },
                HusbandCoolDown = source.HusbandCoolDown is null
                    ? null
                    : new ProtoHusbandCoolDown
                    {
                        Guid = source.HusbandCoolDown.Guid.ToString(),
                        HusbandId = source.HusbandCoolDown.HusbandId,
                        Time = Timestamp.FromDateTime(
                            DateTime.SpecifyKind(source.HusbandCoolDown.Time, DateTimeKind.Utc)
                        ),
                    },
                LastWeddingCongratulatedMonths = source.LastWeddingCongratulatedMonths ?? 0,
                HasLastWeddingCongratulatedMonths = source.LastWeddingCongratulatedMonths.HasValue,
                TwitchUser = source.TwitchUser is null
                    ? null
                    : new ProtoTwitchUser
                    {
                        TwitchId = source.TwitchUser.TwitchId,
                        UserLogin = source.TwitchUser.UserLogin,
                        DisplayName = source.TwitchUser.DisplayName,
                        ProfileImageUrl = source.TwitchUser.ProfileImageUrl ?? string.Empty,
                        ChatColor = source.TwitchUser.ChatColor ?? string.Empty,
                        IsModerator = source.TwitchUser.IsModerator,
                        IsVip = source.TwitchUser.IsVip,
                    },
            };

    private static ProtoWaifu? ToProto(DomainWaifu? source) =>
        source is null
            ? null
            : new ProtoWaifu
            {
                ShikiId = source.ShikiId,
                Name = source.Name,
                Age = source.Age,
                Anime = source.Anime ?? string.Empty,
                Manga = source.Manga ?? string.Empty,
                WhenAdded = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.WhenAdded, DateTimeKind.Utc)
                ),
                LastOrder = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.LastOrder, DateTimeKind.Utc)
                ),
                OrderCount = source.OrderCount,
                IsPrivated = source.IsPrivated,
                ImageUrl = source.ImageUrl,
                AudioId = source.AudioId?.ToString() ?? string.Empty,
                IsMerged = source.IsMerged,
                IsAdded = source.IsAdded,
            };

    private static ProtoFumo? ToProto(DomainFumo? source) =>
        source is null
            ? null
            : new ProtoFumo
            {
                MfcId = source.MfcId,
                Name = source.Name,
                Character = source.Character,
                CharacterTranslit = source.CharacterTranslit ?? string.Empty,
                Origin = source.Origin ?? string.Empty,
                ThumbnailUrl = source.ThumbnailUrl,
                Rating = source.Rating,
                RatingCount = source.RatingCount,
                WhenAdded = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.WhenAdded, DateTimeKind.Utc)
                ),
                LastOrder = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.LastOrder, DateTimeKind.Utc)
                ),
                OrderCount = source.OrderCount,
            };

    private static ProtoMikuModule? ToProto(DomainMikuModule? source) =>
        source is null
            ? null
            : new ProtoMikuModule
            {
                PageId = source.PageId,
                Title = source.Title,
                JapaneseName = source.JapaneseName ?? string.Empty,
                Designer = source.Designer ?? string.Empty,
                ThumbnailUrl = source.ThumbnailUrl,
                Description = source.Description ?? string.Empty,
                Songs = source.Songs ?? string.Empty,
                WhenAdded = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.WhenAdded, DateTimeKind.Utc)
                ),
                LastOrder = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.LastOrder, DateTimeKind.Utc)
                ),
                OrderCount = source.OrderCount,
            };

    private static ProtoFrog? ToProto(DomainFrog? source) =>
        source is null
            ? null
            : new ProtoFrog
            {
                Pid = source.Pid,
                CommonName = source.CommonName,
                ScientificName = source.ScientificName,
                Family = source.Family ?? string.Empty,
                RussianName = source.RussianName ?? string.Empty,
                ThumbnailUrl = source.ThumbnailUrl,
                Size = source.Size ?? string.Empty,
                Status = source.Status ?? string.Empty,
                Category = source.Category ?? string.Empty,
                Habits = source.Habits ?? string.Empty,
                WhenAdded = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.WhenAdded, DateTimeKind.Utc)
                ),
                LastOrder = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(source.LastOrder, DateTimeKind.Utc)
                ),
                OrderCount = source.OrderCount,
            };

    private static ProtoMikuTrackDto ToProto(MARS.Shared.Contracts.Models.MikuTrackDto source) =>
        new()
        {
            Id = source.Id.ToString(),
            Number = source.Number,
            Artist = source.Artist,
            Title = source.Title,
            Url = source.Url,
            ThumbnailUrl = source.ThumbnailUrl ?? string.Empty,
        };

    private static ProtoShikimoriTitleRef ToProto(
        MARS.Shared.Contracts.Clients.ShikimoriTitleRef source
    ) =>
        new()
        {
            Id = source.Id,
            Name = source.Name ?? string.Empty,
            RussianName = source.RussianName ?? string.Empty,
            Year = source.Year ?? 0,
            HasYear = source.Year.HasValue,
            Url = source.Url,
        };

    private static ProtoCollectionInventory ToProto(
        MARS.Shared.Contracts.Clients.CollectionInventory source
    ) =>
        new()
        {
            Collected = source.Collected,
            Total = source.Total,
            Items =
            {
                source.Items.Select(item => new ProtoCollectionItem
                {
                    Name = item.Name,
                    Count = item.Count,
                }),
            },
        };
}
