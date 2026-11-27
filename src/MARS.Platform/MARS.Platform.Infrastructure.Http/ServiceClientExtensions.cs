using System;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Platform.Infrastructure.Http;

public static class ServiceClientExtensions
{
    public static IServiceCollection AddMarsAudioControllerClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AudioControllerConfiguration>(
            configuration.GetSection(AudioControllerConfiguration.SectionName)
        );

        services.AddHttpClient<IAudioControllerClient, AudioControllerClient>(
            (serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(
                    serviceProvider
                        .GetRequiredService<IOptions<AudioControllerConfiguration>>()
                        .Value.BaseUrl
                );
            }
        );

        return services;
    }

    public static string GetServiceEndpoint(this IOptions<ServiceEndpoints> options, string name) =>
        name switch
        {
            nameof(ServiceEndpoints.MediaStorage) => options.Value.MediaStorage,
            nameof(ServiceEndpoints.WaifuGacha) => options.Value.WaifuGacha,
            nameof(ServiceEndpoints.Alerts) => options.Value.Alerts,
            nameof(ServiceEndpoints.OBS) => options.Value.OBS,
            nameof(ServiceEndpoints.Admin) => options.Value.Admin,
            nameof(ServiceEndpoints.TwitchCore) => options.Value.TwitchCore,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Неизвестный сервис"),
        };
}
