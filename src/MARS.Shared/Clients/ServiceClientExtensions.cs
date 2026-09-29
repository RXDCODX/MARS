using MARS.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Clients;

/// <summary>
/// Регистрация межсервисных HTTP-клиентов. Каждый клиент — типизированный
/// <see cref="HttpClient"/> с базовым адресом из секции <c>ServiceEndpoints</c>,
/// поэтому сокеты переиспользуются (см. замечание про <c>SpotifyAuthService</c>
/// и создание <see cref="HttpClient"/> на каждый вызов).
/// </summary>
public static class ServiceClientExtensions
{
    public const string MediaStorageHttpClientName = "mars-media-storage";
    public const string WaifuGachaHttpClientName = "mars-waifu-gacha";

    public static IServiceCollection AddMarsServiceClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var endpoints =
            configuration.GetSection(ServiceEndpoints.SectionName).Get<ServiceEndpoints>()
            ?? new ServiceEndpoints();

        services
            .AddHttpClient(
                MediaStorageHttpClientName,
                client => client.BaseAddress = new Uri(endpoints.MediaStorage)
            )
            .ConfigurePrimaryHttpMessageHandler(
                () => new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
            );

        services
            .AddHttpClient(
                WaifuGachaHttpClientName,
                client => client.BaseAddress = new Uri(endpoints.WaifuGacha)
            )
            .ConfigurePrimaryHttpMessageHandler(
                () => new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
            );

        return services;
    }

    public static IServiceCollection AddMarsServiceClient<TClient, TImplementation>(
        this IServiceCollection services,
        string httpClientName
    )
        where TClient : class
        where TImplementation : class, TClient
    {
        services
            .AddHttpClient<TClient, TImplementation>(httpClientName)
            .ConfigurePrimaryHttpMessageHandler(
                () => new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
            );

        return services;
    }

    /// <summary>Адрес сервиса по имени, с учётом явно заданной секции конфигурации.</summary>
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
