using MARS.Admin.Application.Services;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Shared.Contracts.Clients;

namespace MARS.Admin.Infrastructure.Shikimori;

/// <summary>Reads Shikimori rate-limit telemetry through the inter-service gRPC client.</summary>
public sealed class ShikimoriRateLimiterService(IMarsGrpcClient grpcClient)
    : IShikimoriRateLimiterService
{
    public async Task<ShikimoriRateLimiterInfo?> GetRateLimiterInfoAsync(
        CancellationToken cancellationToken = default
    )
    {
        var info = await grpcClient.Shikimori.GetRateLimiterInfoAsync(cancellationToken);
        return info;
    }
}
