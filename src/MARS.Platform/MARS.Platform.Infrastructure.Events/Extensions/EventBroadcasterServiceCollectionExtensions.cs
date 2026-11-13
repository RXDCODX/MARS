using Microsoft.Extensions.DependencyInjection;

namespace MARS.Platform.Infrastructure.Events;

public static class EventBroadcasterServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует широковещатель событий как одиночный экземпляр.
    /// </summary>
    /// <remarks>
    /// Одиночный, а не scoped: подписчики (реле SignalR-хаба, фоновые службы)
    /// живут дольше запроса, и второй экземпляр раздавал бы события в пустоту.
    /// </remarks>
    public static IServiceCollection AddMarsEventBroadcaster<TMessage>(
        this IServiceCollection services
    )
        where TMessage : class
    {
        services.AddSingleton<EventBroadcaster<TMessage>>();

        return services;
    }
}
