using System.ComponentModel.DataAnnotations;

namespace MARS.Admin.Entities;

public class EnvironmentVariable
{
    [Key]
    public required string Key { get; set; }

    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
