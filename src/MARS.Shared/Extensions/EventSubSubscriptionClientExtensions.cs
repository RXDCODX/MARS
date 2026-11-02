using MARS.Shared.Configuration;
using MARS.Shared.Grpc.EventSub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Shared.Extensions;

/// <summary>Регистрация типизированного клиента подписок Twitch EventSub.</summary>
public static class EventSubSubscriptionClientExtensions
{
    /// <summary>
    /// Регистрирует клиент EventSub, направляя все запросы владельцу Twitch-подключения
    /// <see cref="ServiceEndpoints.TwitchCore"/>. Адрес и gRPC-порт разрешаются общим
    /// механизмом <see cref="GrpcClientExtensions"/>.
    /// </summary>
    public static IServiceCollection AddMarsEventSubSubscriptionClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMarsGrpcClient<EventSubSubscriptionService.EventSubSubscriptionServiceClient>(
            configuration,
            nameof(ServiceEndpoints.TwitchCore)
        );
        services.AddTransient<IEventSubSubscriptionClient, EventSubSubscriptionClient>();

        return services;
    }
}
