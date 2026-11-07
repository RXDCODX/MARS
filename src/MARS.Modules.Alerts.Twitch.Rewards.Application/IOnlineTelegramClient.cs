using TL;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application;

/// <summary>
/// Клиент Telegram для чтения сообщений каналов.
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
/// </summary>
public interface IOnlineTelegramClientFactory
{
    Task<IOnlineTelegramClient> CreateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Минимальная граница над WTelegram.Client, позволяющая тестировать логику
/// авторизации и делегирования без запуска сетевого клиента.
/// </summary>
public interface IWTelegramClientAdapter : IDisposable
{
    event Func<UpdatesBase, Task> OnUpdates;

    Task LoginUserIfNeeded();

    Task<string> DownloadFileAsync(Document document, Stream output);

    Task<string> DownloadFileAsync(Photo photo, Stream output, PhotoSize size);
}

/// <summary>
/// Создаёт адаптер над настоящим WTelegram.Client.
/// </summary>
public interface IWTelegramClientAdapterFactory
{
    IWTelegramClientAdapter Create(Func<string, string?> configuration);
}
