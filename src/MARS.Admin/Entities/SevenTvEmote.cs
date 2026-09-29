using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.Admin.Entities;

[Table("SevenTvEmotes")]
public class SevenTvEmote
{
    [Key]
    [MaxLength(100)]
    public required string Name { get; set; }

    public DateTime LoadedAt { get; set; } = DateTime.Now;
}
