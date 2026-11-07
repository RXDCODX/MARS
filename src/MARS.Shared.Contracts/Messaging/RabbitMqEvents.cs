using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Событие погашения награды канала. Единственное reward-событие шины:
/// конкретная награда определяется по <see cref="RewardTitle"/>, а не по
/// отдельному exchange/routing key.
/// </summary>
public sealed class RewardRedeemedEvent : IMarsEventMessage
{
    public string RewardId { get; set; } = string.Empty;
    public string RewardTitle { get; set; } = string.Empty;
    public int Cost { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserInput { get; set; }
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
    public string? MessageId { get; set; }
    public TwitchUser? User { get; set; }

    /// <summary>
    /// Медиа-контент награды, если награда привязана к файлу.
    /// Заполняет публикующая сторона (MARS.TwitchCore) через
    /// <c>ITwitchMediaPreparationService</c>. Потребитель MARS.Alerts
    /// (<c>TwitchMediaAlerts</c>) читает именно это поле.
    /// </summary>
    public MediaInfo? Media { get; set; }
}

/// <summary>
/// Событие обычного сообщения чата.
/// </summary>
public sealed class ChatEventMessageEvent : IMarsEventMessage
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsModerator { get; set; }
    public bool IsVip { get; set; }
    public bool IsBroadcaster { get; set; }
    public string? ChatColor { get; set; }

    /// <summary>
    /// Идентификатор награды, если сообщение отправлено через награду с вводом
    /// текста. У обычных сообщений чата поле не заполнено.
    /// </summary>
    public string? CustomRewardId { get; set; }

    public int Bits { get; set; }
    public double BitsInDollars { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string? EmoteReplacedMessage { get; set; }
    public bool IsFirstMessage { get; set; }
    public bool IsHighlighted { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string BotUsername { get; set; } = string.Empty;
    public DateTimeOffset TmiSent { get; set; } = DateTimeOffset.UnixEpoch;
    public bool IsSkippingSubMode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Сообщение, отправленное через награду с вводом текста. Отдельный контракт,
/// потому что в оверлей чата такое сообщение не попадает, но по нему
/// поднимается привязанный к награде алерт (AC.S08). Потребители обычного чата
/// не должны видеть такие сообщения.
/// </summary>
public sealed class RewardInputChatMessageEvent : IMarsEventMessage
{
    public ChatEventMessageEvent Message { get; set; } = new();
}

/// <summary>
/// Запрос на отправку сообщения в чат. Публикуют сервисы, у которых нет собственного
/// IRC-подключения; обрабатывает MARS.TwitchCore — владелец единственного
/// подключения бот-аккаунта.
/// </summary>
public sealed class ChatSendEvent : IMarsEventMessage
{
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
}
