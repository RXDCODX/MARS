using MARS.Shared.Configuration;

namespace MARS.Shared.Tests.Configuration;

/// <summary>
/// Имена служб из <c>docker-compose.yml</c>: нужен, чтобы не заводить цель
/// наблюдаемости сервису, которого в compose нет.
/// </summary>
/// <remarks>
/// MARS.AudioController — единственный сервис, который не контейнеризуется. Без
/// сверки с compose он получил бы файл цели для Prometheus, и тот опрашивал бы
/// порт, который никогда не отвечает: в списке целей навсегда висела бы строка
/// «down» рядом с настоящими.
/// </remarks>
public class ComposeServicesTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory(
        "mars-compose-services-"
    );

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Раздел <c>services</c> — единственный источник имён служб: у сетев и томов
    /// отступ такой же, и без слежения за разделом они попали бы в тот же список.
    /// </summary>
    [Fact]
    public void Читает_службы_раздела_services()
    {
        var services = Read(
            """
            services:
              gateway:
                <<: *service-defaults
              shikimori:
                image: mars/shikimori

            networks:
              mars-network:
                driver: bridge

            volumes:
              postgres_data:
            """
        );

        Assert.Equal(["gateway", "shikimori"], Sorted(services));
    }

    /// <summary>
    /// Служба из dev-оверрайда тоже служба: он добавляет блоки, которых нет в
    /// основном файле, а проверка «есть ли такой сервис в compose» обязана видеть
    /// оба файла — так же, как их видит сам compose.
    /// </summary>
    [Fact]
    public void Читает_службы_обоих_Файлов()
    {
        File.WriteAllText(
            Path.Combine(_root.FullName, "docker-compose.yml"),
            "services:\n  gateway:\n"
        );
        File.WriteAllText(
            Path.Combine(_root.FullName, "docker-compose.dev.yml"),
            "services:\n  gateway:\n  shikimori:\n"
        );

        var services = ComposeServices.Read(_root.FullName);

        Assert.Equal(["gateway", "shikimori"], Sorted(services));
    }

    /// <summary>
    /// Каталога с compose может не быть вовсе: тогда список пуст, и решение
    /// «регистрировать цель или нет» принимает вызывающий.
    /// </summary>
    [Fact]
    public void Без_файлов_список_пуст()
    {
        Assert.Empty(ComposeServices.Read(_root.FullName));
    }

    /// <summary>Каталога может не быть вообще — вызывающий передаёт путь от корня вверх.</summary>
    [Fact]
    public void Без_каталога_список_пуст()
    {
        Assert.Empty(ComposeServices.Read(Path.Combine(_root.FullName, "нет-такого")));
    }

    private HashSet<string> Read(string content)
    {
        File.WriteAllText(Path.Combine(_root.FullName, "docker-compose.yml"), content);

        return ComposeServices.Read(_root.FullName);
    }

    /// <summary>
    /// Порядок имён в compose не значит ничего, а набор — HashSet: сравнение
    /// списком проверяло бы ещё и порядок чтения файла, к чему отношения не имеет.
    /// </summary>
    private static List<string> Sorted(IEnumerable<string> services) =>
        services.OrderBy(service => service, StringComparer.Ordinal).ToList();
}
