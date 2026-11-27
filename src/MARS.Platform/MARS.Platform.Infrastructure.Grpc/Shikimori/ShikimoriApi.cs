using Grpc.Core;
using Grpc.Net.ClientFactory;
using DomainShikimori = MARS.Shared.Contracts.Clients;
using ProtoShikimori = MARS.Shared.Contracts.Shikimori;

namespace MARS.Platform.Infrastructure.Grpc.Shikimori;

internal sealed class ShikimoriApi(GrpcClientFactory clientFactory) : IShikimoriApi
{
    public async Task<DomainShikimori.ShikimoriTitleRef?> GetRandomAnimeAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await clientFactory
                .CreateClient<ProtoShikimori.ShikimoriService.ShikimoriServiceClient>("Shikimori")
                .GetRandomAnimeAsync(
                    new ProtoShikimori.EmptyRequest(),
                    cancellationToken: cancellationToken
                );

            return ToModel(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<DomainShikimori.ShikimoriTitleRef?> GetRandomMangaAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await clientFactory
                .CreateClient<ProtoShikimori.ShikimoriService.ShikimoriServiceClient>("Shikimori")
                .GetRandomMangaAsync(
                    new ProtoShikimori.EmptyRequest(),
                    cancellationToken: cancellationToken
                );

            return ToModel(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<DomainShikimori.ShikimoriCharacterRef?> GetCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await clientFactory
                .CreateClient<ProtoShikimori.ShikimoriService.ShikimoriServiceClient>("Shikimori")
                .GetCharacterAsync(
                    new ProtoShikimori.GetCharacterRequest { Id = id },
                    cancellationToken: cancellationToken
                );

            return ToModel(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<DomainShikimori.ShikimoriRateLimiterInfo> GetRateLimiterInfoAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await clientFactory
            .CreateClient<ProtoShikimori.ShikimoriService.ShikimoriServiceClient>("Shikimori")
            .GetRateLimiterInfoAsync(
                new ProtoShikimori.EmptyRequest(),
                cancellationToken: cancellationToken
            );

        return ToModel(response);
    }

    private static DomainShikimori.ShikimoriTitleRef ToModel(
        ProtoShikimori.ShikimoriTitleRef source
    ) =>
        new(
            source.Id,
            string.IsNullOrWhiteSpace(source.Name) ? null : source.Name,
            string.IsNullOrWhiteSpace(source.RussianName) ? null : source.RussianName,
            source.HasYear ? source.Year : null,
            source.Url
        );

    private static DomainShikimori.ShikimoriCharacterRef ToModel(
        ProtoShikimori.ShikimoriCharacterRef source
    ) =>
        new(
            source.Id,
            source.Name,
            string.IsNullOrWhiteSpace(source.RussianName) ? null : source.RussianName,
            string.IsNullOrWhiteSpace(source.Description) ? null : source.Description,
            source.ImageUrl,
            source.ImagePath,
            string.IsNullOrWhiteSpace(source.AnimeTitle) ? null : source.AnimeTitle,
            string.IsNullOrWhiteSpace(source.MangaTitle) ? null : source.MangaTitle
        );

    private static DomainShikimori.ShikimoriRateLimiterInfo ToModel(
        ProtoShikimori.ShikimoriRateLimiterInfo source
    ) =>
        new(
            source.AvailablePerSecond,
            source.AvailablePerMinute,
            source.SecondsToResetSecondWindow,
            source.SecondsToResetMinuteWindow
        );
}
