using MARS.Shared.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace MARS.Shared.Tests.Logging;

/// <summary>
/// Файловый сток логов для сервиса, запущенного на хосте.
/// </summary>
/// <remarks>
/// <para>
/// Логи контейнеров в Loki доставляет Alloy, читая их stdout через Docker-демон.
/// Вывод процесса, запущенного по F5 или <c>scripts/*/local -Run</c>, stdout
/// контейнера не является и в Loki не попадает — то есть у отлаживаемого сервиса
/// в Grafana не было логов вовсе.
/// </para>
/// <para>
/// Поэтому в этом режиме сервис пишет те же строки ещё и в файл, а Alloy читает
/// каталог. Сток включается ключом <c>Dev:LogDirectory</c>, который задаёт только
/// профиль <c>MARS.Local</c>: в контейнерах файлового стока нет вовсе.
/// </para>
/// </remarks>
public sealed class SerilogFileSinkTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("mars-log-sink-");

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Имя файла — имя службы compose, а не имя проекта: лейбл <c>container</c> у
    /// логов с хоста обязан совпадать с лейблом логов из контейнера, иначе запрос
    /// <c>{container="telegram"}</c> показывал бы половину строк сервиса.
    /// </summary>
    [Fact]
    public void Логи_На_Хосте_Пишутся_В_Файл_С_Именем_Службы_Compose()
    {
        var directory = Path.Combine(_root.FullName, "local-logs");

        using (var logger = Logger(directory, "MARS.TwitchCore"))
        {
            logger.Information("строка, которая должна уехать в Loki");
        }

        var file = Assert.Single(Directory.GetFiles(directory, "twitch-core*.log"));

        Assert.Contains("строка, которая должна уехать в Loki", File.ReadAllText(file));
    }

    /// <summary>
    /// Без ключа файлового стока нет: в контейнерах и в боевом стенде лишний файл
    /// на каждый лог только занимал бы место в контейнере.
    /// </summary>
    [Fact]
    public void Без_Каталога_Логов_Файла_Не_Появляется()
    {
        var directory = Path.Combine(_root.FullName, "local-logs");

        using (var logger = Logger(directory: null, "MARS.Telegram"))
        {
            logger.Information("строка в консоль");
        }

        Assert.False(Directory.Exists(directory));
    }

    /// <summary>
    /// Экспорт в Tempo выключен пустым значением: тест не должен ходить в сеть,
    /// а без <c>Otlp:Endpoint</c> настройка и не создаёт ничего.
    /// </summary>
    private static Serilog.ILogger Logger(string? directory, string serviceName)
    {
        var settings = new Dictionary<string, string?> { ["Otlp:Endpoint"] = string.Empty };

        if (directory is not null)
        {
            settings["Dev:LogDirectory"] = directory;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new LoggerConfiguration()
            .AddMarsLogging(configuration, serviceName)
            .CreateLogger();
    }
}