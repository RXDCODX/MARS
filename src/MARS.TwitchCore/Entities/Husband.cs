using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.TwitchCore.Entities;

[Table("Husbands")]
public class Husband
{
    [Key]
    [Required]
    [MaxLength(50)]
    public required string TwitchId { get; set; }

    [ForeignKey(nameof(TwitchId))]
    public TwitchUser? TwitchUser { get; set; }

    public bool IsPrivated { get; set; }

    public DateTime? WhenPrivated { get; set; }

    public int? LastWeddingCongratulatedMonths { get; set; }
}
