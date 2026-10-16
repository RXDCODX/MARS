using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
}
