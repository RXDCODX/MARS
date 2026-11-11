using MARS.TestKit;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Контракт публичной поверхности сборки: каждый публичный тип создаётся, каждое
/// читаемое и записываемое свойство возвращает положенное значение обратно, все
/// статические инициализаторы отрабатывают без исключений.
/// </summary>
public class PublicContractTests
{
    [Fact]
    public void PublicTypesAreConstructibleAndPropertiesRoundTrip()
    {
        var assemblies = new[]
        {
            "MARS.MediaStorage",
            "MARS.MediaStorage.Api.Rest",
            "MARS.MediaStorage.Application",
            "MARS.MediaStorage.Domain",
            "MARS.MediaStorage.Infrastructure.Git",
            "MARS.MediaStorage.Infrastructure.Media",
            "MARS.MediaStorage.Infrastructure.Postgres",
            "MARS.MediaStorage.Infrastructure.Telegram",
        };
        var results = assemblies.Select(PublicContractVerifier.VerifyAssembly).ToArray();

        var failures = results
            .Where(result => result.Violations.Count > 0)
            .Select(PublicContractVerifier.Describe)
            .ToArray();

        Assert.True(failures.Length == 0, string.Join(Environment.NewLine, failures));
        Assert.True(
            results.Sum(result => result.TypesInspected) > 0,
            "Проверка не нашла ни одного типа: сборки загрузились, но их типы не перечислены."
        );
    }

    /// <summary>
    /// Даты приводятся к UTC на границе с базой. Без правила любая запись
    /// <c>DateTime.Now</c> в <c>timestamp with time zone</c> падает на живом
    /// PostgreSQL, а правило легко забыть в новом контексте: оно живёт в
    /// переопределении <c>ConfigureConventions</c>, а не в общей настройке.
    /// </summary>
    [Fact]
    public void DatesAreConvertedToUtc()
    {
        Assert.Empty(
            UtcDatesConventionVerifier.Verify("MARS.MediaStorage.Infrastructure.Postgres")
        );
    }
}
