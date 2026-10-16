using MARS.Shared.Development;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Регистрация автостарта инфраструктуры.
/// </summary>
/// <remarks>
/// Шаг зовётся из <c>AddMarsDefaults</c>, то есть в каждом сервисе, и цена ошибки
/// здесь несимметрична: забытая регистрация выключила бы автостарт молча (F5 снова
/// ждёт базу минуту), а лишняя подмена — тихо подменила бы пробу адресов. Поэтому
/// проверяются обе стороны: и что регистрация даёт нужные реализации, и что уже
/// зарегистрированный шаг она не перебивает.
/// </remarks>
public class DevInfrastructureRegistrationTests
{
    /// <summary>Конфигурация из пар «ключ — значение»: секция <c>Dev</c> целиком.</summary>
    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                settings.Select(setting => new KeyValuePair<string, string?>(
                    setting.Key,
                    setting.Value
                ))
            )
            .Build();

    /// <summary>
    /// Реализации проверяются по дескрипторам, а не через <c>GetRequiredService</c>:
    /// шагу нужны <c>IHostEnvironment</c> и логгер, и создание его в этой проверке
    /// означало бы проверку контейнера, а не состава регистраций.
    /// </summary>
    private static void AssertRegistered<TService, TImplementation>(IServiceCollection services) =>
        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(TService)
                && descriptor.ImplementationType == typeof(TImplementation)
        );

    [Fact]
    public void Регистрация_даёт_шаг_пробу_и_раннер()
    {
        var services = new ServiceCollection().AddDevInfrastructure(Configuration());

        AssertRegistered<IDevInfrastructureStarter, DevInfrastructureStarter>(services);
        AssertRegistered<IDevEndpointProbe, TcpEndpointProbe>(services);
        AssertRegistered<IDevProcessRunner, DevProcessRunner>(services);
    }

    /// <summary>
    /// Флаг читается из секции <c>Dev</c>: имя секции — часть контракта с профилем
    /// <c>MARS.Local</c>, и переименование в коде разошлось бы с
    /// <c>scripts/local-launch.conf</c> молча.
    /// </summary>
    [Fact]
    public void Опции_читаются_из_секции_Dev()
    {
        var services = new ServiceCollection().AddDevInfrastructure(
            Configuration(
                ("Dev:AutoStartInfra", "true"),
                ("Dev:InfraServices", "postgres,rabbitmq"),
                ("Dev:InfraWaitTimeoutSeconds", "42")
            )
        );

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DevInfraOptions>>().Value;

        Assert.True(options.AutoStartInfra);
        Assert.Equal(["postgres", "rabbitmq"], options.ServiceNames);
        Assert.Equal(42, options.InfraWaitTimeoutSeconds);
    }

    /// <summary>
    /// <c>TryAdd</c>, а не <c>Add</c>: подменённый шаг обязан пережить регистрацию.
    /// Иначе тест или сам сервис, подставившие свою пробу, получили бы настоящий
    /// docker вместо подставного.
    /// </summary>
    [Fact]
    public void Уже_зарегистрированный_шаг_не_перебивается()
    {
        var services = new ServiceCollection();
        var stub = new StubStarter();

        services.AddSingleton<IDevInfrastructureStarter>(stub);
        _ = services.AddDevInfrastructure(Configuration());

        Assert.DoesNotContain(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IDevInfrastructureStarter)
                && descriptor.ImplementationType == typeof(DevInfrastructureStarter)
        );

        using var provider = services.BuildServiceProvider();

        Assert.Same(stub, provider.GetRequiredService<IDevInfrastructureStarter>());
    }

    /// <summary>Заглушка шага: нужна только чтобы проверить, что регистрацию не перебили.</summary>
    private sealed class StubStarter : IDevInfrastructureStarter
    {
        public void EnsureStarted() { }
    }
}
