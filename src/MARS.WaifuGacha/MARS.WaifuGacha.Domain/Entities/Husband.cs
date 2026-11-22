using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MARS.Shared.Kernel;

namespace MARS.WaifuGacha.Domain.Entities;

[Table(nameof(Husband) + "s")]
public class Husband
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string TwitchId { get; set; }

    public DateTime WhenOrdered { get; set; }
    public string? WaifuBrideId { get; set; }
    public bool IsPrivated { get; set; }
    public long OrderCount { get; set; }
    public string? WaifuRollId { get; set; }
    public DateTime? WhenPrivated { get; set; }
    public required HusbandAutoHello? HusbandGreetings { get; set; }
    public required HusbandCoolDown? HusbandCoolDown { get; set; }
    public int? LastWeddingCongratulatedMonths { get; set; }
    public bool IsAutoHelloEnabled { get; set; }

    /// <summary>
    /// Denormalized Twitch user info (не хранится в БД) — заполняется при отдаче
    /// сущности другому сервису (например, Alerts) через MARS.Twitch REST.
    /// </summary>
    [NotMapped]
    public MARS.Shared.Kernel.TwitchUser? TwitchUser { get; set; }
}
