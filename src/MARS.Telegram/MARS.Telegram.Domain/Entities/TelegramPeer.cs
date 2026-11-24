using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Domain.Entities;

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
