using System.Text.Json;
using MARS.Shared.Development;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Регистрация сервиса, запущенного на хосте, целью Prometheus.
/// </summary>
/// <remarks>
/// <para>
/// Сервис на хосте отдаёт <c>/metrics</c> сам, но в списке целей Prometheus его
/// нет: там перечислены имена контейнеров. Поэтому файл цели пишет сам сервис —
/// так один и тот же шаг работает и для F5, и для <c>scripts/*/local -Run</c>, и
/// в списке оказывается ровно то, что запущено на хосте.
/// </para>
/// <para>
/// Лейбл <c>instance</c> у хостовой цели отличается от контейнерной
/// (<c>telegram-host</c> против <c>telegram</c>): панели дашборда фильтруют по
/// <c>instance</c>, и совпадение имён молча смешало бы метрики отлаживаемого
/// процесса с метриками его контейнера.
/// </para>
/// </remarks>
public sealed class DevPrometheusTargetRegistrarTests : IDisposable
{
    private const string TelegramCompose = "services:\n  telegram:\n  gateway:\n";

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("mars-prom-target-");

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Цель собирается из двух источников: порта, который задаёт профиль запуска
    /// (<c>Dev:MetricsPort</c>), и имени службы compose, выведенного из имени
    /// проекта. Оба обязаны попасть в файл — иначе Prometheus опросит не тот порт
    /// или подпишет график чужим именем.
    /// </summary>
    [Fact]
    public void Пишет_цель_Хоста_С_Инстансом_Сервиса()
    {
        Registrar(metricsPort: "6004").Register();

        using var document = JsonDocument.Parse(File.ReadAllText(TargetPath("MARS.Telegram")));
        var target = document.RootElement[0];
        var labels = target.GetProperty("labels");

        Assert.Equal("host.docker.internal:6004", target.GetProperty("targets")[0].GetString());
        Assert.Equal("telegram-host", labels.GetProperty("instance").GetString());
        Assert.Equal("host", labels.GetProperty("source").GetString());
    }

    /// <summary>
    /// Имя файла — имя проекта: у двух сервисов с одинаковым портом (в dev такого
    /// нет, но правило не должно зависеть от этого) файлы не слились бы в один.
    /// </summary>
    [Fact]
    public void Файл_Называется_Именем_Проекта()
    {
        Registrar(metricsPort: "6004").Register();

        Assert.True(File.Exists(TargetPath("MARS.Telegram")));
    }

    /// <summary>
    /// Файл перезаписывается, а не дополняется: прошлый запуск мог остаться после
    /// смены порта, и дописывание оставило бы в задаче цель на закрытый порт.
    /// </summary>
    [Fact]
    public void Перезаписывает_Прошлую_Цель()
    {
        var registrar = Registrar(metricsPort: "6004");
        var path = TargetPath("MARS.Telegram");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "не json");
        registrar.Register();

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        Assert.Equal(
            "host.docker.internal:6004",
            document.RootElement[0].GetProperty("targets")[0].GetString()
        );
    }

    /// <summary>
    /// Сервис, которого нет в compose, цели не получает: MARS.AudioController
    /// управляет микшером Windows и не контейнеризуется, а постоянно «down» в
    /// списке целей выглядела бы как неисправность стенда.
    /// </summary>
    [Fact]
    public void Сервис_Которого_Нет_В_Compose_Цель_Не_Получает()
    {
        Registrar(metricsPort: "6004", service: "MARS.AudioController", compose: TelegramCompose)
            .Register();

        Assert.False(File.Exists(TargetPath("MARS.AudioController")));
    }

    /// <summary>
    /// Ключа <c>Dev:MetricsPort</c> нет в контейнере и в боевом стенде: там шаг не
    /// делает ничего и файла не создаёт.
    /// </summary>
    [Fact]
    public void Без_Ключа_Порт_Ничего_Не_Пишется()
    {
        Registrar(metricsPort: null).Register();

        Assert.False(Directory.Exists(Path.Combine(_root.FullName, "data", "prometheus-local")));
    }

    /// <summary>
    /// Вне Development шаг молчит: профиль <c>MARS.Local</c> ставит ключ только
    /// для хостового запуска, а в контейнер ключ может попасть извне.
    /// </summary>
    [Fact]
    public void Вне_Development_Ничего_Не_Пишется()
    {
        Registrar(metricsPort: "6004", environment: "Production").Register();

        Assert.False(Directory.Exists(Path.Combine(_root.FullName, "data", "prometheus-local")));
    }

    /// <summary>
    /// Недоступный каталог — предупреждение, а не отказ запуска: подсказка по
    /// отладке не стоит того, чтобы уронить сервис на старте. Иначе забытый
    /// <c>--env-file</c> или права на каталог выглядели бы как «сервис не запускается».
    /// </summary>
    [Fact]
    public void Недоступный_Каталог_Не_Роняет_Запуск()
    {
        File.WriteAllText(Path.Combine(_root.FullName, "data"), "не каталог");

        Registrar(metricsPort: "6004").Register();
    }

    private DevPrometheusTargetRegistrar Registrar(
        string? metricsPort,
        string service = "MARS.Telegram",
        string environment = "Development",
        string? compose = null
    )
    {
        File.WriteAllText(
            Path.Combine(_root.FullName, "docker-compose.yml"),
            compose ?? TelegramCompose
        );

        var settings = metricsPort is null
            ? []
            : new KeyValuePair<string, string?>("Dev:MetricsPort", metricsPort);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection([settings]).Build();

        var host = new Mock<IHostEnvironment>();
        host.SetupGet(environment_ => environment_.EnvironmentName).Returns(environment);
        host.SetupGet(environment_ => environment_.ContentRootPath).Returns(_root.FullName);

        return new DevPrometheusTargetRegistrar(
            service,
            configuration,
            host.Object,
            new Mock<ILogger<DevPrometheusTargetRegistrar>>().Object
        );
    }

    private string TargetPath(string service) =>
        Path.Combine(
            _root.FullName,
            DevPrometheusTargetRegistrar.TargetsDirectory,
            $"{service}.json"
        );
}
