using MARS.Shared.Models;
using MARS.Shared.Models.Media;

namespace MARS.Shared.Messaging;

/// <summary>
/// Внутренний контекст обработки Channel Points redemption.
/// Сам EventSub продолжает передаваться как исходный JSON через один общий маршрут.
/// </summary>
public class RewardRedeemedEvent
{
    public string RewardId { get; set; } = string.Empty;
    public string RewardTitle { get; set; } = string.Empty;

    /// <summary>
    /// Необязательный ключ специального серверного обработчика.
    /// Обычным Twitch-наградам он не нужен.
    /// </summary>
    public string? AlertKey { get; set; }

    /// <summary>Необязательная точная привязка к медиа в MARS.MediaStorage.</summary>
    public Guid? MediaInfoId { get; set; }

    /// <summary>EventSub metadata.message_id для дедупликации.</summary>
    public string? EventSubMessageId { get; set; }

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
    /// <c>IMediaStorageClient</c> по <c>ChannelRewardRecord.MediaInfoId</c>.
    /// Потребитель MARS.Alerts читает именно это поле, а не пытается
    /// десериализовать всё тело события в <see cref="MediaInfo"/> — из-за
    /// несовпадения типов каждое сообщение раньше уходило в ветку
    /// "payload without valid MediaInfo" и молча терялось.
    /// </summary>
    public MediaInfo? Media { get; set; }
}

/// <summary>
/// Просьба вернуть баллы канала за редемпшн.
/// </summary>
/// <remarks>
/// Возврат исполняет только владелец токена Twitch — MARS.TwitchCore: вызов
/// <c>UpdateRedemptionStatus</c> требует авторизации стримера, которой у
/// MARS.Alerts нет. Поэтому MARS.Alerts публикует просьбу, а не пытается
/// вернуть баллы сам. <see cref="RedemptionId"/> — это идентификатор
/// редемпшена (Twitch отдаёт его вместе с событием), а не идентификатор
/// награды: подменить их местами значит отменить чужой редемпшн.
/// </remarks>
public class RewardRefundEvent
{
    /// <summary>Идентификатор награды канала.</summary>
    public string RewardId { get; set; } = string.Empty;

    /// <summary>Идентификатор редемпшена, который надо отменить.</summary>
    public string RedemptionId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    /// <summary>Причина возврата — попадает в журнал, зрителю не показывается.</summary>
    public string? Reason { get; set; }
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
