using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Domain.Entities;

/// <summary>
/// Тип пира Telegram в соглашении Bot API.
/// </summary>
public enum TelegramPeerKind
{
    User = 0,
    Chat = 1,
    Channel = 2,
}
