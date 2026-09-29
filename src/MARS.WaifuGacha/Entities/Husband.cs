using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.WaifuGacha.Entities;

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
}
