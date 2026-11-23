using System.Net.Http;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Twitch.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Twitch.Infrastructure.Http;

public static class TwitchInternalClientExtensions
{
    public const string TwitchInternalHttpClientName = "mars-twitch-internal";

    public static IServiceCollection AddMarsTwitchInternalClient(this IServiceCollection services)
    {
        services
            .AddHttpClient<ITwitchInternalClient, TwitchInternalClient>(
                TwitchInternalHttpClientName,
                (provider, client) =>
                    client.BaseAddress = new Uri(
                        provider.GetRequiredService<IOptions<ServiceEndpoints>>().Value.TwitchCore
                    )
            )
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
            );

        return services;
    }
}
