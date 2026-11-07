using TL;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Клиент Telegram для чтения сообщений каналов.
///
/// За интерфейсом — только то, что вызывает сбор мемов. Настоящий
/// <c>WTelegram.Client</c> авторизуется по номеру телефона и держит сессию на
/// диске, поэтому в тесте не собирается: конструктор уходит в сеть.
/// </summary>
public interface IOnlineTelegramClient : IDisposable
{
    /// <summary>
    /// Обновления приходят пачками, поэтому обработчик асинхронный.
    /// </summary>
    event Func<UpdatesBase, Task> OnUpdates;

    Task<string> DownloadFileAsync(Document document, Stream output);

    Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size);
}

/// <summary>
/// Создаёт клиента Telegram для мониторинга каналов.
///
/// Отдельный интерфейс нужен ради одного вызова: так фабрику можно подменить в
/// тесте, не заставляя сервис знать, откуда взялся настоящий клиент.
/// </summary>
public interface IOnlineTelegramClientFactory
{
    Task<IOnlineTelegramClient> CreateAsync(CancellationToken cancellationToken = default);
}
