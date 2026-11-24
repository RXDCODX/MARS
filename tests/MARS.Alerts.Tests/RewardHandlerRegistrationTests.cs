using MARS.Alerts.Application.TwitchMessages;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Listeners;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.TestKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Util;

namespace MARS.Alerts.Tests;

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
            options.DescribeHandlerMatch(typeof(TwitchRewardInputMessageListener))
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

    [Fact]
    public void AddMarsRewardHandlers_AddsOneRegistrationPerHandler()
    {
        var services = new ServiceCollection();
        var handlers = RewardHandlerRegistration.DiscoverHandlerTypes();

        services.AddMarsRewardHandlers();
        var registrations = services.Count(descriptor =>
            descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
        );

        Assert.Equal(handlers.Count + 1, registrations);
    }

    [Fact]
    public void AddMarsRewardHandlers_RegistersEveryHandlerAsIRewardAlertHandler()
    {
        var services = new ServiceCollection();
        services.AddMarsRewardHandlers();

        var interfaceRegistrations = services
            .Where(d => d.ServiceType == typeof(IRewardAlertHandler))
            .ToList();
        var discovered = RewardHandlerRegistration.DiscoverHandlerTypes();

        Assert.Equal(discovered.Count, interfaceRegistrations.Count);
        Assert.All(interfaceRegistrations, d => Assert.NotNull(d.ImplementationFactory));
    }
}
