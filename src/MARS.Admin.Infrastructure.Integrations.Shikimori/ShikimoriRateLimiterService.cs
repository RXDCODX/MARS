using MARS.Admin.Application.Services;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Shared.Contracts.Clients;

namespace MARS.Admin.Infrastructure.Integrations.Shikimori;

/// <summary>Reads Shikimori rate-limit telemetry through the platform HTTP client.</summary>
public sealed class ShikimoriRateLimiterService(IShikimoriApiClient shikimoriClient)
    : IShikimoriRateLimiterService
{
    public Task<ShikimoriRateLimiterInfo?> GetRateLimiterInfoAsync(
        CancellationToken cancellationToken = default
    ) => shikimoriClient.GetRateLimiterInfoAsync(cancellationToken);
}
