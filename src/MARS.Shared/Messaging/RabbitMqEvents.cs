using System.Text.Json;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Wolverine.Attributes;

namespace MARS.Shared.Messaging;

/// <summary>
/// Псевдонимы сообщений шины. Псевдоним — это имя обменника RabbitMQ, он же
/// <c>message-type</c> в заголовках Wolverine.
/// </summary>
/// <remarks>
/// Все контракты собраны здесь, потому что псевдоним обязан быть уникальным:
/// два типа с одинаковым псевдонимом легли бы в один обменник, и каждый
/// потребитель получал бы чужие сообщения. Проверка уникальности — в
/// <c>WolverineRegistrationTests</c>.
/// </remarks>
public static class MarsMessageAliases
{
    public const string RewardRedeemed = "mars.reward.redeemed";
    public const string ChatMessageReceived = "mars.chat.message.received";
    public const string RewardInputMessage = "mars.chat.message.reward-input";
    public const string ChatMessageDeleted = "mars.chat.message.deleted";
    public const string ChatSend = "mars.chat.send";
    public const string EventSubRaw = "mars.eventsub.raw";
}

/// <summary>
/// Погашение награды Twitch. Публикует MARS.TwitchCore, читают MARS.Alerts и
/// MARS.WaifuGacha.
/// </summary>
[MessageIdentity(MarsMessageAliases.RewardRedeemed)]
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
/// Общие поля сообщения чата. Само по себе сообщением шины не является:
/// публикуются конкретные типы-наследники, иначе обычный чат и сообщение из
/// награды попали бы в одну очередь, и потребитель ответил бы на награду как на
/// обычное сообщение.
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

/// <summary>Обычное сообщение чата.</summary>
[MessageIdentity(MarsMessageAliases.ChatMessageReceived)]
public class ChatMessageReceivedEvent : ChatMessageEvent;

/// <summary>
/// Сообщение, отправленное через награду с вводом текста. В оверлей чата такое
/// сообщение не попадает, но по нему поднимается привязанный к награде алерт
/// (AC.S08).
/// </summary>
[MessageIdentity(MarsMessageAliases.RewardInputMessage)]
public class RewardInputMessageEvent : ChatMessageEvent;

/// <summary>Удаление сообщения в чате.</summary>
[MessageIdentity(MarsMessageAliases.ChatMessageDeleted)]
public class ChatMessageDeletedEvent
{
    public string MessageId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Запрос на отправку сообщения в чат. Публикуют сервисы, у которых нет
/// собственного IRC-подключения; обрабатывает MARS.TwitchCore — владелец
/// единственного подключения бот-аккаунта (блокер №19 из аудита PR).
/// </summary>
[MessageIdentity(MarsMessageAliases.ChatSend)]
public class ChatSendEvent
{
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
}

/// <summary>
/// Сырой кадр EventSub. Потребителей в репозитории нет: контракт нужен, чтобы
/// кадр шёл типизированным сообщением, а не анонимным словарём.
/// </summary>
[MessageIdentity(MarsMessageAliases.EventSubRaw)]
public class EventSubRawEvent
{
    public int SchemaVersion { get; set; } = 1;
    public string? MessageId { get; set; }
    public string MessageType { get; set; } = "unknown";
    public string? SubscriptionType { get; set; }
    public string? SubscriptionVersion { get; set; }
    public string? SubscriptionId { get; set; }
    public string? SessionId { get; set; }
    public string EventName { get; set; } = "unknown";
    public DateTimeOffset ObservedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string SourceService { get; set; } = "MARS.TwitchCore";
    public string? TraceParent { get; set; }
    public JsonElement Raw { get; set; }
}
