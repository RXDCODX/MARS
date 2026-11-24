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

/// <summary>
/// Постоянная запись о пире Telegram: идентификатор, тип, <c>access_hash</c>,
/// имя/заголовок. Используется для резолва адресата без повторного обхода
/// диалогов после рестарта процесса.
/// </summary>
public class TelegramPeer
{
    [Key]
    public long Id { get; set; }

    public int PeerKind { get; set; }

    public long AccessHash { get; set; }

    public string? Username { get; set; }

    public string? Title { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
