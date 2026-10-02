using System.Reflection;
using MARS.Alerts.Services.Twitch.Rewards;
using MARS.Shared.Messaging;
using MARS.TestKit;

namespace MARS.Alerts.Tests;

/// <summary>
/// Все обработчики наград отвечают на своё событие.
///
/// Обработчиков больше сорока, и у каждого своя награда, свой текст и свой
/// вызов вниз. Проверять их по одному бессмысленно: одинаково ломается одно и то
/// же — обработчик без routing key (его не признает ни один фильтр
/// потребителя), обработчик, который падает на пустом событии (а событие из
/// RabbitMQ приходит вовсе не таким), и обработчик, который не собирается.
/// </summary>
public class RewardHandlerSuiteTests
{
    [Fact]
    public void EveryHandlerDeclaresRoutingKeyAndHandlesEvent()
    {
        var failures = new List<string>();
        var handled = 0;

        foreach (var type in HandlerTypes())
        {
            if (Stub.Resolve(type) is not IRewardAlertHandler handler)
            {
                failures.Add($"{type.Name}: не собирается заглушками.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(handler.RoutingKey))
            {
                failures.Add($"{type.Name}: пустой routing key — потребитель его не заберёт.");
            }

            var failure = TryHandle(handler);
            if (failure is null)
            {
                handled++;
            }
            else
            {
                failures.Add($"{type.Name}.HandleAsync: {failure}");
            }
        }

        Assert.True(
            handled > 20,
            $"Обработано только {handled} обработчиков — обход проверил бы пустоту."
        );
        Assert.Empty(failures);
    }

    /// <summary>
    /// Разные routing key не должны совпадать: иначе два обработчика окажутся
    /// на одном ключе, и один из них молча перестанет вызываться — фильтр
    /// выбирает первый подходящий.
    /// </summary>
    [Fact]
    public void RoutingKeysAreUniquePerReward()
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var type in HandlerTypes())
        {
            if (Stub.Resolve(type) is not IRewardAlertHandler handler)
            {
                continue;
            }

            if (keys.TryGetValue(handler.RoutingKey, out var owner))
            {
                Assert.Fail(
                    $"routing key «{handler.RoutingKey}» объявлен и у {owner}, и у {type.Name}."
                );
            }

            keys[handler.RoutingKey] = type.Name;
        }

        Assert.NotEmpty(keys);
    }

    private static string? TryHandle(IRewardAlertHandler handler)
    {
        string? failure = null;

        try
        {
            handler
                .HandleAsync(SampleEvent(), TestContext.Current.CancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception exception)
        {
            failure = $"{exception.GetType().Name}: {exception.Message.Split('\n')[0]}";
        }

        return failure;
    }

    private static RewardRedeemedEvent SampleEvent() =>
        new()
        {
            RewardId = "reward-1",
            RewardTitle = "Награда",
            Cost = 100,
            UserId = "42",
            UserName = "pyro",
            UserInput = "проверка",
            RedeemedAt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            MessageId = "job-1",
        };

    private static List<Type> HandlerTypes() =>
        [
            .. typeof(IRewardAlertHandler)
                .Assembly.GetExportedTypes()
                .Where(type =>
                    typeof(IRewardAlertHandler).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsGenericTypeDefinition
                )
                .OrderBy(type => type.FullName, StringComparer.Ordinal),
        ];
}
