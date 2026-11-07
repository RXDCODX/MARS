using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Contracts.Messaging;

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
