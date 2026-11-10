using Microsoft.Extensions.DependencyInjection;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Регистрация всех обработчиков наград разом.
/// </summary>
/// <remarks>
/// Заменяет перечисление <c>AddRewardHandler&lt;T&gt;()</c> вручную: пока каждый
/// новый хендлер нужно было ещё и дописать в <c>Program.cs</c>, его можно было
/// забыть, и награда молча уходила в счётчик
/// <c>rabbitmq_unhandled_messages</c> без обработчика. Теперь список берётся
/// из сборки, а ключи наград проверяются тестами на непустоту и уникальность.
///
/// Все конкретные обработчики наследуются от <see cref="RewardHandlerBase"/>;
/// сам базовый класс реализует <see cref="IRewardAlertHandler"/> как контракт
/// маршрутизации редемпшенов.
/// </remarks>
public static class RewardHandlerRegistration
{
    /// <summary>
    /// Регистрирует каждый конкретный <see cref="RewardHandlerBase"/> из сборки
    /// <c>MARS.Alerts.Application.Twitch.Rewards</c> как singleton.
    /// </summary>
    /// <remarks>
    /// Если конкретный тип уже зарегистрирован, берётся именно тот экземпляр,
    /// чтобы все потребители одного обработчика разделяли его состояние.
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
    /// Типы наследников <see cref="RewardHandlerBase"/>, которые регистрирует
    /// <see cref="AddMarsRewardHandlers"/>.
    /// Отдельный метод — чтобы тест проверял тот же список, что и composition root.
    /// </summary>
    public static IReadOnlyList<Type> DiscoverHandlerTypes()
    {
        var result = typeof(RewardHandlerBase)
            .Assembly.GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                && typeof(RewardHandlerBase).IsAssignableFrom(type)
            )
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        return result;
    }
}
