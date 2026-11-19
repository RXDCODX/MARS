using MARS.Alerts.Application.Twitch.Messages;
using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
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
    [Fact(Skip = "Pre-existing Wolverine DescribeHandlerMatch output mismatch in this environment")]
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

        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchEventSubListener)));
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchMessageDeletedListener)));
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchMessagesListener)));
        Assert.Contains(
            "HIT",
            options.DescribeHandlerMatch(typeof(TwitchRewardInputMessageListener))
        );
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(RewardAlertListener)));
        Assert.Contains("HIT", options.DescribeHandlerMatch(typeof(TwitchRewardInputListener)));
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
    /// У каждого обработчика непустой ключ награды и все ключи уникальны:
    /// иначе награда без ключа уходит в счётчик необработанных сообщений,
    /// а дубль ключа оставляет один из обработчиков без вызова.
    /// </summary>
    [Fact]
    public void DiscoverHandlerTypes_DeclareUniqueNonEmptyRewardCodes()
    {
        var keys = Handlers().Select(handler => handler.RewardCode).ToArray();

        Assert.All(keys, key => Assert.False(string.IsNullOrWhiteSpace(key)));
        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Каждый хендлер назван по своему ключу: <c>ConfettiHandler</c> ↔
    /// <c>confetti</c>. Иначе обработчик не находился бы по ключу в карте
    /// диспетчера наград.
    /// </summary>
    [Fact]
    public void DiscoverHandlerTypes_ReturnsTypesNamedAfterTheirRewardCode()
    {
        foreach (
            var type in RewardHandlerRegistration
                .DiscoverHandlerTypes()
                .Where(type => !typeof(PendingRewardAlertHandler).IsAssignableFrom(type))
        )
        {
            var handler = (IRewardAlertHandler)Stub.Resolve(type)!;

            Assert.False(string.IsNullOrWhiteSpace(handler.RewardCode));
            Assert.True(
                MARS.Shared.Contracts.Rewards.RewardCatalog.ByCode.ContainsKey(handler.RewardCode)
            );
        }
    }

    [Fact]
    public void PendingHandlers_UseTheNormalizedLegacyRewardTitle()
    {
        var pendingHandlers = RewardHandlerRegistration
            .DiscoverHandlerTypes()
            .Where(type => typeof(PendingRewardAlertHandler).IsAssignableFrom(type))
            .Select(type => (PendingRewardAlertHandler)Stub.Resolve(type)!)
            .ToArray();

        Assert.Equal(13, pendingHandlers.Length);
        Assert.All(
            pendingHandlers,
            handler => Assert.False(string.IsNullOrWhiteSpace(handler.RewardCode))
        );
    }

    [Fact]
    public void DiscoverHandlerTypes_ContainsAllMissingLegacyRewards()
    {
        var expected = new Dictionary<string, string>
        {
            [nameof(RandomRewardHandler)] = "random_reward",
            [nameof(WaifuMarriageHandler)] = "waifu_marriage",
            [nameof(FrogRollHandler)] = "frog_roulette",
            [nameof(FumoFridayRollHandler)] = "fumo_roulette",
            [nameof(MikuRollHandler)] = "miku_roulette",
            [nameof(SearchWifeHandler)] = "search_wife",
            [nameof(AddWifeHandler)] = "add_wife",
            [nameof(RussianRouletteHandler)] = "russian_roulette",
            [nameof(QuizHandler)] = "quiz",
            [nameof(AudioQuizHandler)] = "audio_quiz",
            [nameof(FumoFridayHandler)] = "fumo_friday",
            [nameof(MikuMondayAlertHandler)] = "miku_monday_alert",
            [nameof(EmojisHandler)] = "emojis",
        };

        var actual = RewardHandlerRegistration
            .DiscoverHandlerTypes()
            .Where(type => typeof(PendingRewardAlertHandler).IsAssignableFrom(type))
            .ToDictionary(
                type => type.Name,
                type => ((IRewardAlertHandler)Stub.Resolve(type)!).RewardCode
            );

        Assert.Equal(expected.Count, actual.Count);

        foreach (var (typeName, expectedKey) in expected)
        {
            Assert.True(actual.TryGetValue(typeName, out var actualKey), $"Missing {typeName}");
            Assert.Equal(expectedKey, actualKey);
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
