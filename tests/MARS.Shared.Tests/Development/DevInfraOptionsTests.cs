using MARS.Shared.Development;
using Microsoft.Extensions.Configuration;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Настройки автостарта инфраструктуры.
/// </summary>
/// <remarks>
/// Списки служб приходят одной строкой через запятую
/// (<c>Dev__InfraServices=postgres,rabbitmq</c>) — ровно так их пишет профиль
/// MARS.Local. Ключи значения — строчные: конфигурационный биндер умеет приводить
/// типы, но не умеет разбивать скалярный ключ на массив, поэтому списки и
/// разбираются здесь, а не в <c>Bind</c>.
/// </remarks>
public class DevInfraOptionsTests
{
    private static DevInfraOptions Bind(params (string Name, string Value)[] values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                values.ToDictionary(
                    value => $"{DevInfraOptions.SectionName}:{value.Name}",
                    value => (string?)value.Value
                )
            )
            .Build();

        var options = new DevInfraOptions();
        configuration.GetSection(DevInfraOptions.SectionName).Bind(options);

        return options;
    }

    [Fact]
    public void Автостарт_выключен_по_умолчанию()
    {
        var options = new DevInfraOptions();

        Assert.False(options.AutoStartInfra);
    }

    /// <summary>
    /// Настраиваемые значения доходят до опций: если списки и число остались бы
    /// дефолтными, профиль перестал бы быть источником и правка в
    /// <c>local-launch.conf</c> не делала бы ничего.
    /// </summary>
    [Fact]
    public void Списки_служб_читаются_через_запятую()
    {
        var options = Bind(
            ("AutoStartInfra", "true"),
            ("InfraServices", "postgres,rabbitmq"),
            ("InfraBackgroundServices", "tempo,grafana"),
            ("InfraWaitTimeoutSeconds", "120")
        );

        Assert.True(options.AutoStartInfra);
        Assert.Equal(new[] { "postgres", "rabbitmq" }, options.ServiceNames);
        Assert.Equal(new[] { "tempo", "grafana" }, options.BackgroundServiceNames);
        Assert.Equal(120, options.InfraWaitTimeoutSeconds);
    }

    [Fact]
    public void Лишние_пробелы_и_пустые_элементы_отбрасываются()
    {
        var options = Bind(
            ("InfraServices", " postgres , , rabbitmq"),
            ("InfraBackgroundServices", ",tempo,,")
        );

        Assert.Equal(new[] { "postgres", "rabbitmq" }, options.ServiceNames);
        Assert.Equal(new[] { "tempo" }, options.BackgroundServiceNames);
    }

    /// <summary>
    /// Пустой список — пустой массив, а не исключение на старте: значение в
    /// профиле редактируется руками.
    /// </summary>
    [Fact]
    public void Пустой_список_разбирается_в_пустой_массив()
    {
        var options = Bind(("InfraServices", "  "), ("InfraBackgroundServices", ""));

        Assert.Empty(options.ServiceNames);
        Assert.Empty(options.BackgroundServiceNames);
    }

    /// <summary>
    /// Дефолт — это боевой набор служб, а не пустые списки: секция без явных
    /// значений не должна оставить сервис без базы и наблюдаемости.
    /// </summary>
    [Fact]
    public void Дефолты_перечисляют_базу_брокера_и_наблюдаемость()
    {
        var options = new DevInfraOptions();

        Assert.Contains("postgres", options.ServiceNames);
        Assert.Contains("rabbitmq", options.ServiceNames);
        Assert.Contains("tempo", options.BackgroundServiceNames);
        Assert.True(options.InfraWaitTimeoutSeconds > 0);
    }
}
