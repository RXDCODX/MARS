using System.ComponentModel.DataAnnotations;

namespace MARS.MediaStorage.Entities.DTOs;

public class MemeOrderDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }

    [Required(ErrorMessage = "FilePath is required")]
    public string FilePath { get; set; } = string.Empty;

    public int? MemeTypeId { get; set; }
    public MemeTypeDto? Type { get; set; }
}
