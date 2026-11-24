namespace MARS.Telegram.Application.Models;

/// <summary>
/// Состояние MTProto-подключения самого бота (а не пользовательского аккаунта).
/// </summary>
/// <remarks>
/// Пользовательский аккаунт живёт в <see cref="WTelegramClientStatus"/> и его
/// статус страница уже показывает. Этот тип нужен, чтобы отличить «страница
/// открылась» от «бот действительно подключён к Telegram и принимает обновления».
/// </remarks>
public sealed class TelegramBotStatus
{
    /// <summary>Подключён ли бот (клиент авторизован и держит сессию).</summary>
    public bool IsConnected { get; init; }

    /// <summary>Идентификатор бота в Telegram, если подключён.</summary>
    public long? BotUserId { get; init; }

    /// <summary>Username бота (без @), если подключён.</summary>
    public string? BotUsername { get; init; }

    /// <summary>Текст последней ошибки, если не подключён.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Когда состояние проверялось.</summary>
    public DateTime CheckedAtUtc { get; init; }
}
