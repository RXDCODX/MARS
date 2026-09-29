using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.WaifuGacha.Entities;

[Table(nameof(HusbandCoolDown) + "s")]
public class HusbandCoolDown
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public required string HusbandId { get; set; }

    [ForeignKey(nameof(HusbandId))]
    public Husband? Husband { get; set; }
    public DateTime Time { get; set; }
}
