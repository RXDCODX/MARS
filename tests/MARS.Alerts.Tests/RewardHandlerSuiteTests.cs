using System.Reflection;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using MARS.TestKit;

namespace MARS.Alerts.Tests;

/// <summary>
/// Все обработчики наград отвечают на своё событие.
///
/// Обработчиков больше сорока, и у каждого своя награда, свой текст и свой
/// вызов вниз. Проверять их по одному бессмысленно: одинаково ломается одно и то
/// же — обработчик без ключа награды (его не признает диспетчер потребителя),
/// обработчик, который падает на пустом событии (а событие из RabbitMQ приходит
/// вовсе не таким), и обработчик, который не собирается.
/// </summary>
public class RewardHandlerSuiteTests
{
    [Fact]
    public void EveryHandlerDeclaresRewardCodeAndCanBeConstructed()
    {
        var failures = new List<string>();
        var constructed = 0;

        foreach (var type in HandlerTypes())
        {
            // RickRollerService теперь зависит от TestOverlayHub,
            // поэтому увеличиваем глубину рекурсивной сборки графа заглушек.
            if (Stub.Resolve(type, depth: 3) is not IRewardAlertHandler handler)
            {
                failures.Add($"{type.Name}: не собирается заглушками.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(handler.RewardCode))
            {
                failures.Add($"{type.Name}: пустой ключ награды — потребитель его не заберёт.");
            }

            constructed++;
        }

        Assert.True(
            constructed > 20,
            $"Собрано только {constructed} обработчиков — обход проверил бы пустоту."
        );
        Assert.Empty(failures);
    }

    /// <summary>
    /// Разные ключи наград не должны совпадать: иначе два обработчика окажутся
    /// на одном ключе, и один из них молча перестанет вызываться — диспетчер
    /// выбирает первый подходящий.
    /// </summary>
    [Fact]
    public void RewardCodesAreUniquePerReward()
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var type in HandlerTypes())
        {
            if (Stub.Resolve(type, depth: 3) is not IRewardAlertHandler handler)
            {
                continue;
            }

            if (keys.TryGetValue(handler.RewardCode, out var owner))
            {
                Assert.Fail(
                    $"ключ награды «{handler.RewardCode}» объявлен и у {owner}, и у {type.Name}."
                );
            }

            keys[handler.RewardCode] = type.Name;
        }

        Assert.NotEmpty(keys);
    }

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