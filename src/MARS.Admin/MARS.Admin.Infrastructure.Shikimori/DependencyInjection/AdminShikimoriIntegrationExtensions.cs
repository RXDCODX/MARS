using MARS.Admin.Application.Services;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Admin.Infrastructure.Shikimori.DependencyInjection;

public static class AdminShikimoriIntegrationExtensions
{
    public static IServiceCollection AddAdminShikimoriIntegration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddMarsGrpcClient();
        services.AddScoped<IShikimoriRateLimiterService, ShikimoriRateLimiterService>();

        return services;
    }
}
