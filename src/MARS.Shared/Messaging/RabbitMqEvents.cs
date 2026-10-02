using MARS.Shared.Models;
using MARS.Shared.Models.Media;

namespace MARS.Shared.Messaging;

/// <summary>
/// Событие RabbitMQ для reward redemption.
/// Используется как payload при публикации reward events.
/// </summary>
public class RewardRedeemedEvent
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
    /// (<c>TwitchMediaAlerts</c>) читает именно это поле, а не пытается
    /// десериализовать всё тело события в <see cref="MediaInfo"/> — из-за
    /// несовпадения типов каждое сообщение раньше уходило в ветку
    /// "payload without valid MediaInfo" и молча терялось.
    /// </summary>
    public MediaInfo? Media { get; set; }
}

/// <summary>
/// Событие RabbitMQ для chat messages.
/// </summary>
public class ChatMessageEvent
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsModerator { get; set; }
    public bool IsVip { get; set; }
    public bool IsBroadcaster { get; set; }
    public string? ChatColor { get; set; }

    /// <summary>
    /// Идентификатор награды, если сообщение отправлено через награду с вводом
    /// текста. У обычных сообщений чата поле не заполнено.
    /// </summary>
    public string? CustomRewardId { get; set; }
}

/// <summary>
/// Событие RabbitMQ для user joined.
/// </summary>
public class UserJoinedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Запрос на отправку сообщения в чат. Публикуют сервисы, у которых нет собственного
/// IRC-подключения; обрабатывает MARS.TwitchCore — владелец единственного
/// подключения бот-аккаунта (блокер №19 из аудита PR).
/// </summary>
public class ChatSendEvent
{
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
}

/// <summary>
/// Событие RabbitMQ для track events.
/// </summary>
public class TrackEvent
{
    public Guid TrackId { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? RequestedByUserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Событие RabbitMQ для waifu roll.
/// </summary>
public class WaifuRollEvent
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int WaifuId { get; set; }
    public string WaifuName { get; set; } = string.Empty;
    public string? WaifuImageUrl { get; set; }
    public bool IsNewWaifu { get; set; }
    public DateTime RolledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Событие RabbitMQ для fumo/frog/miku roll.
/// </summary>
public class CollectionRollEvent
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string RollType { get; set; } = string.Empty; // fumo, frog, miku
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemImageUrl { get; set; }
    public bool IsNew { get; set; }
    public DateTime RolledAt { get; set; } = DateTime.UtcNow;
}
