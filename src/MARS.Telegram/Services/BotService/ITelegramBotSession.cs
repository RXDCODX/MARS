using MARS.Telegram.Services.Messaging;

namespace MARS.Telegram.Services.BotService;

/// <summary>
/// Бот как источник подключения для остальных служб сервиса.
/// </summary>
/// <remarks>
/// Клиент один на сервис: он держит сессию в базе, подписан на обновления, и
/// второй экземпляр того же бота зашёл бы под другой сессией. Наружу отдаётся не
/// сам клиент, а узкая поверхность <see cref="ITelegramBotConnection"/> — иначе
/// каждый обработчик зависел бы от всей библиотеки и от её невиртуальных методов.
/// </remarks>
public interface ITelegramBotSession
{
    /// <summary>
    /// Авторизованное подключение. Вход ленивый и выполняется один раз.
    /// </summary>
    Task<ITelegramBotConnection> GetAsync(CancellationToken cancellationToken);
}
