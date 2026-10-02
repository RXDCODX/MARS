using MARS.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// HTTP-клиент <c>MARS.Shikimori</c> — единственного владельца клиента
/// Shikimori и рейт-лимитера к нему.
/// </summary>
public class ShikimoriApiClient(
    HttpClient httpClient,
    IOptions<ServiceAuthOptions> authOptions,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<ShikimoriApiClient> logger
) : ServiceHttpClientBase(httpClient, authOptions, logger), IShikimoriApiClient
{
    public override string ServiceEndpoint => endpoints.Value.Shikimori;

    public Task<ShikimoriTitleRef?> GetRandomAnimeAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<ShikimoriTitleRef>("api/Shikimori/random-anime", cancellationToken);
    }

    public Task<ShikimoriTitleRef?> GetRandomMangaAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<ShikimoriTitleRef>("api/Shikimori/random-manga", cancellationToken);
    }

    public Task<ShikimoriCharacterRef?> GetCharacterAsync(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<ShikimoriCharacterRef>($"api/Shikimori/characters/{id}", cancellationToken);
    }

    public Task<ShikimoriRateLimiterInfo?> GetRateLimiterInfoAsync(
        CancellationToken cancellationToken = default
    )
    {
        return GetAsync<ShikimoriRateLimiterInfo>("api/Shikimori/rate-limiter", cancellationToken);
    }
}
