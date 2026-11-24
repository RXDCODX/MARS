using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Domain.Entities;

/// <summary>
/// Метка «прочитано» для входящих сообщений Telegram: последний обработанный
/// идентификатор сообщения в конкретном чате. По ней сервис отличает уже
/// обработанные сообщения от непрочитанных (например, при повторной доставке
/// после сбоя) и не выполняет команды повторно.
/// </summary>
/// <remarks>
/// Идентификатор чата хранится в соглашении Bot API (положительный —
/// пользователь, отрицательный — группа/канал): так он совпадает с
/// <c>TelegramIncomingMessage.ChatId</c> и ключами конфигурации.
/// </remarks>
public class TelegramChatReadState
{
    [Key]
    public long ChatId { get; set; }

    /// <summary>Тип пира: 0 — пользователь, 1 — группа, 2 — канал/супергруппа.</summary>
    public int PeerKind { get; set; }

    /// <summary>Последний обработанный идентификатор сообщения (0 — ничего).</summary>
    public int LastReadMessageId { get; set; }

    public DateTimeOffset LastReadAt { get; set; }
}
