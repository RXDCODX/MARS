using MARS.TestKit;

namespace MARS.Telegram.Tests;

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

        // Обёртка WTelegram собирается из настоящего клиента, а его конструктор
        // проверяет api_id и api_hash и падает без них: проверка контракта
        // дошла бы до сети и проверяла бы не тип, а WTelegram. Переадресация
        // вызовов проверяется точечными тестами через IWTelegramChannelClient.
        options.IgnoredTypes.Add(
            "MARS.Telegram.Services.PrivateChannelsResender.WTelegramChannelClient"
        );

        var result = PublicContractVerifier.VerifyAssembly("MARS.Telegram", options);

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
        Assert.Empty(UtcDatesConventionVerifier.Verify("MARS.Telegram"));
    }
}
