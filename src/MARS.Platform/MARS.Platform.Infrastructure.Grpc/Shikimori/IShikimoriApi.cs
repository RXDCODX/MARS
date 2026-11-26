using DomainShikimori = MARS.Shared.Contracts.Clients;

namespace MARS.Platform.Infrastructure.Grpc.Shikimori;

public interface IShikimoriApi
{
    Task<DomainShikimori.ShikimoriTitleRef?> GetRandomAnimeAsync(CancellationToken cancellationToken = default);

    Task<DomainShikimori.ShikimoriTitleRef?> GetRandomMangaAsync(CancellationToken cancellationToken = default);

    Task<DomainShikimori.ShikimoriCharacterRef?> GetCharacterAsync(long id, CancellationToken cancellationToken = default);

    Task<DomainShikimori.ShikimoriRateLimiterInfo> GetRateLimiterInfoAsync(CancellationToken cancellationToken = default);
}