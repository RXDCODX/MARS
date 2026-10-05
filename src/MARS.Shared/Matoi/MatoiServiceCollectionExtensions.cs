using MARS.Shared.Matoi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Matoi;

/// <summary>
/// Регистрация клиента matoi. Вызывается сервисами, которым нужны посты booru:
/// MARS.Alerts (награда RANDOM ART) и MARS.Telegram.
/// </summary>
/// <remarks>
/// Отдельное расширение, а не строка в <c>ServiceClientExtensions</c>: адрес
/// matoi намеренно не в <c>ServiceEndpoints</c>. Он не межсервисный — это шлюз к
/// booru, наружу не публикуется и не имеет маршрута в YARP, а отражение
/// построителя карты Swagger обошло бы его как ещё одну конечную точку.
/// </remarks>
public static class MatoiServiceCollectionExtensions
{
    public const string HttpClientName = "mars-matoi";

    public static IServiceCollection AddMatoiClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<MatoiOptions>(configuration.GetSection(MatoiOptions.SectionName));

        services
            .AddHttpClient(
                HttpClientName,
                (provider, client) =>
                {
                    var settings = provider.GetRequiredService<IOptions<MatoiOptions>>().Value;

                    if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
                    {
                        client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
                    }

                    client.Timeout = TimeSpan.FromSeconds(
                        Math.Clamp(settings.TimeoutSeconds, 1, 120)
                    );
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
            );

        // AddHttpClient<TClient, TImplementation> регистрирует клиент во временном
        // окружении: он держит HttpClient, а тот не должен жить дольше обработчика.
        // Экономия идёт не на экземпляре, а на соединениях — обработчик общий.
        services.AddHttpClient<IMatoiPostService, MatoiPostService>(HttpClientName);

        return services;
    }
}
