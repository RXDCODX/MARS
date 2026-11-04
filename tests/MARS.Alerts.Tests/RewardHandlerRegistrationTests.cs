using MARS.Alerts.Services.Twitch.Rewards;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Alerts.Tests;

/// <summary>
/// Регистрация обработчиков наград берётся из сборки, а не из списка в
/// <c>Program.cs</c>: инвариант «один хендлер на один ключ» проверяется здесь,
/// иначе забытый при регистрации хендлер тихо уводил награду в счётчик
/// необработанных сообщений.
/// </summary>
public class RewardHandlerRegistrationTests
{
    [Fact]
    public void DiscoverHandlerTypes_ReturnsOnlyConcreteImplementations()
    {
        var handlers = RewardHandlerRegistration.DiscoverHandlerTypes();

        Assert.NotEmpty(handlers);
        Assert.All(
            handlers,
            type =>
            {
                Assert.True(typeof(IRewardAlertHandler).IsAssignableFrom(type));
                Assert.True(type.IsClass);
                Assert.False(type.IsAbstract);
                Assert.False(type.IsGenericTypeDefinition);
            }
        );
    }

    /// <summary>
    /// Повторный вызов регистрации не должен удваивать набор хендлеров: он
    /// вызывается один раз, но список в <c>Program.cs</c> легко было бы
    /// продублировать при копировании блока.
    /// </summary>
    [Fact]
    public void AddMarsRewardHandlers_AddsOneRegistrationPerHandler()
    {
        var services = new ServiceCollection();
        var handlers = RewardHandlerRegistration.DiscoverHandlerTypes();

        services.AddMarsRewardHandlers();
        var registrations = services.Count(descriptor =>
            descriptor.ServiceType == typeof(IRewardAlertHandler)
        );

        Assert.Equal(handlers.Count, registrations);
    }
}
