using MARS.TestKit;

namespace MARS.Shikimori.Tests;

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
            "MARS.Shikimori.Domain",
            "MARS.Shikimori.Application",
            "MARS.Shikimori.Infrastructure",
            "MARS.Shikimori.Host",
        };

        foreach (var assembly in assemblies)
        {
            var result = PublicContractVerifier.VerifyAssembly(assembly);

            Assert.True(
                result.TypesInspected > 0,
                $"{assembly}: проверка не нашла ни одного публичного типа."
            );
            Assert.True(
                result.Violations.Count == 0,
                $"{assembly}:{Environment.NewLine}{PublicContractVerifier.Describe(result)}"
            );
        }
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
        Assert.Empty(UtcDatesConventionVerifier.Verify("MARS.Shikimori.Infrastructure"));
    }
}
