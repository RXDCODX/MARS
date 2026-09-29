using System.ComponentModel.DataAnnotations;

namespace MARS.WaifuGacha.Entities;

public class RootState
{
    [Key]
    public required string Name { get; set; }

    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TypeDescription { get; set; } = string.Empty;
}
