using MARS.TestKit;

namespace MARS.Alerts.Tests;

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
        var result = PublicContractVerifier.VerifyAssembly("MARS.Alerts");
        var adhdApplication = PublicContractVerifier.VerifyAssembly(
            "MARS.Modules.Alerts.Adhd.Application"
        );
        var adhdInfrastructure = PublicContractVerifier.VerifyAssembly(
            "MARS.Modules.Alerts.Adhd.Postgres.Infrastructure"
        );

        Assert.Empty(result.Violations);
        Assert.Empty(adhdApplication.Violations);
        Assert.Empty(adhdInfrastructure.Violations);
        Assert.True(
            result.TypesInspected
                + adhdApplication.TypesInspected
                + adhdInfrastructure.TypesInspected
                > 0,
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
            UtcDatesConventionVerifier.Verify("MARS.Modules.Alerts.Adhd.Postgres.Infrastructure")
        );
    }
}
