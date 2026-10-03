using MARS.TestKit;

namespace MARS.TwitchCore.Tests;

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
        var options = new ContractOptions();

        // Реализация доступа к YouTube — единственный тип сервиса, который
        // ссылается на YoutubeExplode: загрузка этой сборки вешает процесс
        // целиком, а не только этот тест. Вызовы проверяются через IYouTubeApi.
        options.IgnoredTypes.Add("MARS.TwitchCore.Services.YouTube.YoutubeExplodeApi");

        var result = PublicContractVerifier.VerifyAssembly("MARS.TwitchCore", options);

        Assert.Empty(result.Violations);
        Assert.True(
            result.TypesInspected > 0,
            "Проверка не нашла ни одного типа: сборка загрузилась, но её типы не перечислены."
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
        Assert.Empty(UtcDatesConventionVerifier.Verify("MARS.TwitchCore"));
    }
}
