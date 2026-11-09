using System.ComponentModel.DataAnnotations;

namespace MARS.Telegram.Domain.Entities;

public class WTelegramSession
{
    public const string DefaultSessionName = "Default";

    [Key]
    public string Name { get; set; } = DefaultSessionName;

    public byte[] Data { get; set; } = [];
}
