using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.Http.MediaArtifact;

public static class MediaArtifactServiceCollectionExtensions
{
    public const string HttpClientName = MediaArtifactDownloader.HttpClientName;

    public static IServiceCollection AddMediaArtifactDownloader(this IServiceCollection services)
    {
        services
            .AddHttpClient(
                HttpClientName,
                client =>
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                }
            );

        services.AddHttpClient<IMediaArtifactDownloader, MediaArtifactDownloader>(HttpClientName);
        return services;
    }
}
