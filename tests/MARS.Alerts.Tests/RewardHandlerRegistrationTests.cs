using MARS.Modules.Alerts.Twitch.Messages.Application;
using MARS.Modules.Alerts.Twitch.Rewards.Application;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using MARS.TestKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Util;

namespace MARS.Alerts.Tests;

/// <summary>
/// Регистрация обработчиков наград берётся из сборки, а не из списка в
/// <c>Program.cs</c>: инвариант «один хендлер на одну награду» проверяется
/// здесь, иначе забытый при регистрации хендлер тихо уводил награду в
/// счётчик необработанных сообщений.
/// </summary>
public class RewardHandlerRegistrationTests
{
    [Fact]
    public void AddMarsWolverine_DiscoversListenersFromAdditionalAssemblies()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["RabbitMq:Host"] = "localhost";

        builder.AddMarsWolverine(
            "MARS.Alerts",
            typeof(MARS.Alerts.Program).Assembly,
            typeof(TwitchEventSubListener).Assembly,
            typeof(RewardAlertListener).Assembly
        );

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<WolverineOptions>();

        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchEventSubListener)));
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchMessageDeletedListener)));
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchMessagesListener)));
        Assert.Contains(
            "HIT",
            options.DescribeHandlerMatch(typeof(TwitchRewardInputMessageListener))
        );
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(RewardAlertListener)));
    }

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
    /// У каждого обработчика непустой ключ награды и все ключи уникальны:
    /// иначе награда без ключа уходит в счётчик необработанных сообщений,
    /// а дубль ключа оставляет один из обработчиков без вызова.
    /// </summary>
    [Fact]
    public void DiscoverHandlerTypes_DeclareUniqueNonEmptyRewardKeys()
    {
        var keys = Handlers().Select(handler => handler.RewardKey).ToArray();

        Assert.All(keys, key => Assert.False(string.IsNullOrWhiteSpace(key)));
        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Каждый хендлер назван по своему ключу: <c>ConfettiHandler</c> ↔
    /// <c>confetti</c>. Иначе обработчик не находился бы по ключу в карте
    /// диспетчера наград.
    /// </summary>
    [Fact]
    public void DiscoverHandlerTypes_ReturnsTypesNamedAfterTheirRewardKey()
    {
        foreach (var type in RewardHandlerRegistration.DiscoverHandlerTypes())
        {
            var handler = (IRewardAlertHandler)Stub.Resolve(type)!;

            Assert.Equal(
                handler.RewardKey,
                type.Name.Replace("Handler", string.Empty, StringComparison.Ordinal),
                ignoreCase: true
            );
        }
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

    private static IReadOnlyList<IRewardAlertHandler> Handlers() =>
        [
            .. RewardHandlerRegistration
                .DiscoverHandlerTypes()
                .Select(type => (IRewardAlertHandler)Stub.Resolve(type)!),
        ];
}
