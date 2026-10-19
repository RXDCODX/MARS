using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Shared.Development;

/// <summary>
/// Регистрация автостарта инфраструктуры для хостового запуска сервиса.
/// </summary>
/// <remarks>
/// <para>
/// Регистрируется в каждом сервисе (через <c>AddMarsDefaults</c>), но сама
/// регистрация ещё ничего не делает: решение принимает
/// <see cref="DevInfrastructureStarter"/> — по флагу <c>Dev:AutoStartInfra</c>,
/// окружению и локальности адресов из конфигурации. В контейнере секции <c>Dev</c>
/// нет, поэтому вызов не даёт ничего.
/// </para>
/// <para>
/// <c>TryAdd</c>, а не <c>Add</c>: тест и, при необходимости, сам сервис должны
/// иметь возможность подставить свою пробу адресов или свой раннер процесса.
/// </para>
/// </remarks>
public static class DevInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Опции автостарта, проба адресов, раннер процесса и шаг поднятия.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">
    /// Конфигурация сервиса: из секции <see cref="DevInfraOptions.SectionName"/>
    /// читаются флаг и списки служб.
    /// </param>
    /// <remarks>
    /// Секции <c>Dev</c> может не быть вовсе (запуск не из профиля
    /// <c>MARS.Local</c>), и тогда работает умолчание самой опции — см.
    /// <see cref="DevInfraOptions.AutoStartInfra"/>.
    /// </remarks>
    public static IServiceCollection AddDevInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<DevInfraOptions>(configuration.GetSection(DevInfraOptions.SectionName));

        services.TryAddSingleton<IDevEndpointProbe, TcpEndpointProbe>();
        services.TryAddSingleton<IDevProcessRunner, DevProcessRunner>();
        services.TryAddSingleton<IDevInfrastructureStarter, DevInfrastructureStarter>();

        return services;
    }

    /// <summary>
    /// Регистрация объявления цели Prometheus для процесса, запущенного на хосте.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="serviceName">
    /// Имя проекта (<c>MARS.TwitchCore</c>): по нему выводится имя службы compose,
    /// которым подписывается цель и который Alloy кладёт в лейбл <c>container</c>
    /// файла логов.
    /// </param>
    /// <remarks>
    /// Отдельный метод, а не ещё одна строка в <c>AddDevInfrastructure</c>: имя
    /// проекта там не нужно, а шаг к наблюдаемости не поднимает контейнеры и
    /// потому не должен делить с автостартом ни флаг, ни списки служб.
    /// </remarks>
    public static IServiceCollection AddDevObservability(
        this IServiceCollection services,
        string serviceName
    )
    {
        services.TryAddSingleton<IDevPrometheusTargetRegistrar>(
            provider => new DevPrometheusTargetRegistrar(
                serviceName,
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<IHostEnvironment>(),
                provider.GetRequiredService<ILogger<DevPrometheusTargetRegistrar>>()
            )
        );

        return services;
    }
}
