using MARS.Modules.Alerts.Twitch.Messages.Application;
using MARS.Modules.Alerts.Twitch.Rewards.Application;
using MARS.Platform.Messaging.RabbitMq.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace MARS.Alerts.Tests;

/// <summary>
/// Listener-ы Twitch находятся в отдельных feature-сборках.
/// Проверяем discovery через общий composition API, чтобы перенос файлов
/// не оставил их за пределами сканирования Wolverine.
/// </summary>
public sealed class TwitchListenerDiscoveryTests
{
    [Fact]
    public void AddMarsWolverineDiscoversListenersFromBothFeatureAssemblies()
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
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(ChatUserListener)));
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchMediaAlertsListener)));
    }
}
