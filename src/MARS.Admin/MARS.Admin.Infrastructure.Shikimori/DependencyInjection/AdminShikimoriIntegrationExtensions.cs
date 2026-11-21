using MARS.Admin.Application.Services;
using MARS.Platform.Infrastructure.Http;
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
        services.AddMarsServiceClients(configuration);
        services.AddMarsServiceClient<IShikimoriApiClient, ShikimoriApiClient>(
            ServiceClientExtensions.ShikimoriHttpClientName
        );
        services.AddScoped<IShikimoriRateLimiterService, ShikimoriRateLimiterService>();

        return services;
    }
}
