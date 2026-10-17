using MARS.Shared.Development;

namespace MARS.Shared.Tests.Development;

/// <summary>
/// Сверка файла окружения с шаблоном.
/// </summary>
/// <remarks>
/// Файлы здесь настоящие, во временном каталоге: предмет проверки — чтение строк
/// и то, что наружу уходят только имена ключей, а значения рабочего файла (то
/// есть секреты владельца) — нет.
/// </remarks>
public class EnvironmentFileParityTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("mars-parity-");

    public void Dispose()
    {
        _directory.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private string Write(string name, params string[] lines)
    {
        var path = Path.Combine(_directory.FullName, name);
        File.WriteAllLines(path, lines);

        return path;
    }

    [Fact]
    public void Ключи_берутся_до_первого_равно_и_комментарии_не_ключи()
    {
        var keys = EnvironmentFileParity.Keys([
            "# MARS_PORT_GATEWAY=7001 — комментарий, а не ключ",
            string.Empty,
            "MARS_PORT_GATEWAY=7001",
            "  MARS_PORT_POSTGRES = 6017 ",
            "строка без равно",
            "=",
        ]);

        Assert.Equal(["MARS_PORT_GATEWAY", "MARS_PORT_POSTGRES"], keys);
    }

    [Fact]
    public void Отсутствующие_ключи_называются_по_имени_и_без_значений()
    {
        var template = Write(
            ".env.development.example",
            "MARS_PORT_GATEWAY=7001",
            "MARS_PORT_POSTGRES=6017",
            "MARS_PORT_RABBITMQ=6018"
        );
        var file = Write(
            ".env.development",
            "MARS_PORT_GATEWAY=7001",
            "MARS_PORT_POSTGRES=secret-password-value"
        );

        var missing = EnvironmentFileParity.Missing(file, template);

        Assert.Equal(["MARS_PORT_RABBITMQ"], missing);
        Assert.DoesNotContain(missing, name => name.Contains("password", StringComparison.Ordinal));
    }

    [Fact]
    public void Список_отсортирован_и_без_повторов()
    {
        var template = Write(
            ".env.development.example",
            "MARS_PORT_RABBITMQ=6018",
            "MARS_PORT_GATEWAY=7001",
            "MARS_PORT_RABBITMQ=6018"
        );
        var file = Write(".env.development", "MARS_PORT_POSTGRES=6017");

        var missing = EnvironmentFileParity.Missing(file, template);

        Assert.Equal(["MARS_PORT_GATEWAY", "MARS_PORT_RABBITMQ"], missing);
    }

    [Fact]
    public void Совпадающие_файлы_ничего_не_теряют()
    {
        var template = Write(
            ".env.development.example",
            "MARS_PORT_GATEWAY=7001",
            "MARS_PORT_MATOI=6027"
        );
        var file = Write(".env.development", "MARS_PORT_MATOI=6027", "MARS_PORT_GATEWAY=7001");

        Assert.Empty(EnvironmentFileParity.Missing(file, template));
    }

    /// <summary>
    /// Файла или шаблона нет — сравнивать не с чем, и это не повод считать, что
    /// всё пропало: отсутствие файла окружения проверяет сам стартер, отдельным
    /// сообщением.
    /// </summary>
    [Fact]
    public void Сравнивать_нечего_когда_файла_или_шаблона_нет()
    {
        var template = Write(".env.development.example", "MARS_PORT_GATEWAY=7001");
        var absent = Path.Combine(_directory.FullName, "нет-такого-файла");

        Assert.Empty(EnvironmentFileParity.Missing(absent, template));
        Assert.Empty(EnvironmentFileParity.Missing(template, absent));
    }
}
