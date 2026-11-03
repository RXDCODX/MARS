using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Alerts.Tests;

/// <summary>
/// Регистрация обработчиков наград берётся из сборки, а не из списка в
/// <c>Program.cs</c>: инвариант «один хендлер на один routing key» проверяется
/// здесь, иначе забытый при регистрации хендлер тихо уводил награду в
/// счётчик необработанных сообщений.
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
    /// Инвариант из раздела 6.3 чеклиста: столько же routing key'ов в
    /// <c>RewardSpecificKeys</c>, сколько хендлеров в сборке. Расхождение
    /// означает либо награду без хендлера, либо хендлер без ключа.
    /// </summary>
    [Fact]
    public void DiscoverHandlerTypes_CountMatchesTheBoundRoutingKeys()
    {
        var handlers = RewardHandlerRegistration.DiscoverHandlerTypes();

        Assert.Equal(RabbitMqConfig.RewardSpecificKeys.Length, handlers.Count);
    }

    /// <summary>
    /// Каждый хендлер назван по своему routing key'у: <c>ConfettiHandler</c> ↔
    /// <c>twitch.reward.confetti</c>. Иначе обработчик не находился бы по
    /// ключу в карте <c>RewardAlertConsumer</c>. Сравнение без учёта регистра:
    /// ключи в <c>RabbitMqConfig</c> строятся из имени награды в нижнем регистре,
    /// а имя класса начинается с заглавной.
    /// </summary>
    [Fact]
    public void DiscoverHandlerTypes_ReturnsTypesNamedAfterTheirRoutingKey()
    {
        var expected = RabbitMqConfig
            .RewardSpecificKeys.Select(key =>
                $"{key[(RabbitMqConfig.RewardPrefix.Length)..]}Handler"
            )
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var actual = RewardHandlerRegistration
            .DiscoverHandlerTypes()
            .Select(type => type.Name)
            .ToArray();

        Assert.Empty(expected.Except(actual, StringComparer.OrdinalIgnoreCase));
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
