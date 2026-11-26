using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using DomainShikimori = MARS.Shared.Contracts.Clients;
using ProtoShikimori = MARS.Shared.Contracts.Shikimori;
using MARS.Shikimori.Application.Ports;
using MARS.Shikimori.Application.Services;
using Microsoft.Extensions.Logging;

namespace MARS.Shikimori.Host.Grpc;

public sealed class ShikimoriGrpcService(
    MARS.Shikimori.Application.Services.ShikimoriService shikimoriService,
    IShikimoriCatalog catalog
) : ProtoShikimori.ShikimoriService.ShikimoriServiceBase
{
    public override async Task<ProtoShikimori.ShikimoriTitleRef> GetRandomAnime(ProtoShikimori.EmptyRequest request, ServerCallContext context)
    {
        var anime = await shikimoriService.GetRandomAnimeAsync(context.CancellationToken);
        if (anime is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Shikimori не вернул аниме"));
        }

        await catalog.RecordPickAsync("anime", anime, context.CancellationToken);
        return ToProto(anime);
    }

    public override async Task<ProtoShikimori.ShikimoriTitleRef> GetRandomManga(ProtoShikimori.EmptyRequest request, ServerCallContext context)
    {
        var manga = await shikimoriService.GetRandomMangaAsync(context.CancellationToken);
        if (manga is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Shikimori не вернул мангу"));
        }

        await catalog.RecordPickAsync("manga", manga, context.CancellationToken);
        return ToProto(manga);
    }

    public override async Task<ProtoShikimori.ShikimoriCharacterRef> GetCharacter(ProtoShikimori.GetCharacterRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "id персонажа должен быть положительным"));
        }

        var character = await shikimoriService.GetCharacterAsync(request.Id, context.CancellationToken);
        if (character is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Shikimori не вернул персонажа {request.Id}"));
        }

        var saved = await catalog.SaveCharacterAsync(character, context.CancellationToken);
        return ToProto(saved);
    }

    public override Task<ProtoShikimori.ShikimoriRateLimiterInfo> GetRateLimiterInfo(ProtoShikimori.EmptyRequest request, ServerCallContext context)
    {
        var info = shikimoriService.GetRateLimiterInfo();
        return Task.FromResult(new ProtoShikimori.ShikimoriRateLimiterInfo
        {
            AvailablePerSecond = info.AvailablePerSecond,
            AvailablePerMinute = info.AvailablePerMinute,
            SecondsToResetSecondWindow = info.SecondsToResetSecondWindow,
            SecondsToResetMinuteWindow = info.SecondsToResetMinuteWindow,
        });
    }

    private static ProtoShikimori.ShikimoriTitleRef ToProto(MARS.Shared.Contracts.Clients.ShikimoriTitleRef source) => new()
    {
        Id = source.Id,
        Name = source.Name ?? string.Empty,
        RussianName = source.RussianName ?? string.Empty,
        Year = source.Year ?? 0,
        HasYear = source.Year.HasValue,
        Url = source.Url,
    };

    private static ProtoShikimori.ShikimoriCharacterRef ToProto(MARS.Shared.Contracts.Clients.ShikimoriCharacterRef source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        RussianName = source.RussianName ?? string.Empty,
        Description = source.Description ?? string.Empty,
        ImageUrl = source.ImageUrl,
        ImagePath = source.ImagePath,
        AnimeTitle = source.AnimeTitle ?? string.Empty,
        MangaTitle = source.MangaTitle ?? string.Empty,
    };
}