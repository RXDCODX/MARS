using MARS.Alerts.Application.TwitchMessages;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Listeners;
using MARS.Platform.Infrastructure.RabbitMq;
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
            typeof(MARS.Alerts.Api.Program).Assembly,
            typeof(TwitchEventSubListener).Assembly,
            typeof(RewardAlertListener).Assembly
        );

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<WolverineOptions>();

        Assert.DoesNotContain(
            "MISS -- Implements",
            options.DescribeHandlerMatch(typeof(TwitchEventSubListener))
        );
        Assert.DoesNotContain(
            "MISS -- Implements",
            options.DescribeHandlerMatch(typeof(TwitchMessageDeletedListener))
        );
        Assert.DoesNotContain(
            "MISS -- Implements",
            options.DescribeHandlerMatch(typeof(TwitchMessagesListener))
        );
        Assert.DoesNotContain(
            "MISS -- Implements",
            options.DescribeHandlerMatch(typeof(TwitchRewardInputMessageListener))
        );
        Assert.DoesNotContain(
            "MISS -- Implements",
            options.DescribeHandlerMatch(typeof(RewardAlertListener))
        );
        Assert.DoesNotContain(
            "MISS -- Implements",
            options.DescribeHandlerMatch(typeof(TwitchRewardInputListener))
        );
        Assert.Contains("Methods:", options.DescribeHandlerMatch(typeof(TwitchEventSubListener)));
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
                Assert.True(typeof(RewardHandlerBase).IsAssignableFrom(type));
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
            descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
        );

        Assert.Equal(handlers.Count, registrations);
    }

    private static IReadOnlyList<IRewardAlertHandler> Handlers() =>
        [
            .. RewardHandlerRegistration
                .DiscoverHandlerTypes()
                .Select(type => (IRewardAlertHandler)Stub.Resolve(type, depth: 3)!),
        ];
}
