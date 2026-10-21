namespace MARS.Shared.Logging.Telegram;

/// <summary>
/// Доставка записи журнала в Telegram.
/// </summary>
/// <remarks>
/// Отдельный интерфейс — не ради абстракции вообще: отправитель умеет ходить в
/// Telegram только по gRPC к <c>MARS.Telegram</c>, и проверять его юнит-тестом
/// без сети можно лишь с заглушкой этого интерфейса.
/// </remarks>
public interface ITelegramLogPublisher : IDisposable
{
    /// <summary>
    /// Отправляет запись.
    /// </summary>
    /// <exception cref="TelegramLogNotConfiguredException">
    /// Telegram не настроен принимать журнал. Повторять доставку бессмысленно:
    /// настройка не появится сама.
    /// </exception>
    Task<TelegramLogPublishResult> PublishAsync(
        TelegramLogEntry entry,
        CancellationToken cancellationToken
    );
}
