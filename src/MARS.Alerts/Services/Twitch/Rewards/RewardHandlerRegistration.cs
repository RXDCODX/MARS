using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Регистрация всех обработчиков наград разом.
/// </summary>
/// <remarks>
/// Заменяет перечисление <c>AddRewardHandler&lt;T&gt;()</c> вручную: пока каждый
/// новый хендлер нужно было ещё и дописать в <c>Program.cs</c>, его можно было
/// забыть, и награда молча уходила в счётчик
/// <c>rabbitmq_unhandled_messages</c> без обработчика. Теперь список берётся
/// из сборки, а тест сверяет его с <c>RabbitMqConfig.RewardSpecificKeys</c>.
///
/// Отражение по <see cref="IRewardAlertHandler"/>, а не по
/// <c>TemporaryReward</c>, как в монолите: там наследников
/// <c>TemporaryReward</c> не осталось ни одного, а награды реализуют именно
/// этот интерфейс.
/// </remarks>
public static class RewardHandlerRegistration
{
    /// <summary>
    /// Регистрирует каждый конкретный <see cref="IRewardAlertHandler"/> из сборки
    /// <c>MARS.Alerts</c> как singleton.
    /// </summary>
    /// <remarks>
    /// Если конкретный тип уже зарегистрирован, берётся именно тот экземпляр:
    /// <c>MikuMikuBeamHandler</c> отдаётся ещё и как
    /// <c>IChatUserTrackingHandler</c>, и два разных объекта означали бы два
    /// разных состояния счётчика участников чата.
    /// </remarks>
    public static IServiceCollection AddMarsRewardHandlers(this IServiceCollection services)
    {
        foreach (var handlerType in DiscoverHandlerTypes())
        {
            services.AddSingleton<IRewardAlertHandler>(sp =>
                sp.GetService(handlerType) as IRewardAlertHandler
                ?? (IRewardAlertHandler)ActivatorUtilities.CreateInstance(sp, handlerType)
            );
        }

        return services;
    }

    /// <summary>
    /// Типы хендлеров, которые регистрирует <see cref="AddMarsRewardHandlers"/>.
    /// Отдельный метод — чтобы тест проверял тот же список, что и composition root.
    /// </summary>
    public static IReadOnlyList<Type> DiscoverHandlerTypes()
    {
        var result = typeof(IRewardAlertHandler)
            .Assembly.GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                && typeof(IRewardAlertHandler).IsAssignableFrom(type)
            )
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        return result;
    }
}
