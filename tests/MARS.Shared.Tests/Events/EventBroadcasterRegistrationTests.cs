using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Shared.Tests.Events;

public class EventBroadcasterRegistrationTests
{
    /// <summary>
    /// Рассылка событий регистрируется один раз на всё приложение: иначе каждый
    /// вызов создавал бы свой канал и подписчики не получали бы событий.
    /// </summary>
    [Fact]
    public void EventBroadcasterIsSingleton()
    {
        var services = new ServiceCollection();

        services.AddMarsEventBroadcaster<TelegramusEvent>();

        using var provider = services.BuildServiceProvider();
        Assert.Same(
            provider.GetRequiredService<EventBroadcaster<TelegramusEvent>>(),
            provider.GetRequiredService<EventBroadcaster<TelegramusEvent>>()
        );
    }
}
