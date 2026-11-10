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
            "MARS.Telegram.Infrastructure.MTProto.PrivateChannelsResender.WTelegramChannelClient"
        );

        // Обёртка бота собирается из настоящего клиента, и проверка контракта
        // подставила бы вместо него экземпляр WTelegram.Client: его конструктор
        // читает api_id и api_hash и разбирает хеш как hex — без настоящих
        // учётных данных проверка падала бы с «The input is not a valid hex
        // string», то есть проверяла бы WTelegram, а не тип сервиса.
        options.IgnoredTypes.Add(
            "MARS.Telegram.Infrastructure.MTProto.Messaging.WTelegramBotConnection"
        );

        var host = PublicContractVerifier.VerifyAssembly("MARS.Telegram.Host", options);
        var domain = PublicContractVerifier.VerifyAssembly("MARS.Telegram.Domain");
        var application = PublicContractVerifier.VerifyAssembly("MARS.Telegram.Application");
        var persistence = PublicContractVerifier.VerifyAssembly(
            "MARS.Telegram.Infrastructure.Postgres"
        );
        var telegram = PublicContractVerifier.VerifyAssembly(
            "MARS.Telegram.Infrastructure.MTProto",
            options
        );
        var googlePhotos = PublicContractVerifier.VerifyAssembly(
            "MARS.Telegram.Infrastructure.GooglePhotos"
        );

        Assert.Empty(host.Violations);
        Assert.Empty(domain.Violations);
        Assert.Empty(application.Violations);
        Assert.Empty(persistence.Violations);
        Assert.Empty(telegram.Violations);
        Assert.Empty(googlePhotos.Violations);
        Assert.True(
            host.TypesInspected
                + domain.TypesInspected
                + application.TypesInspected
                + persistence.TypesInspected
                + telegram.TypesInspected
                + googlePhotos.TypesInspected
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
        Assert.Empty(UtcDatesConventionVerifier.Verify("MARS.Telegram.Infrastructure.Postgres"));
    }
}
