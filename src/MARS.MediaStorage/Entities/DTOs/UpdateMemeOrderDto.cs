using System.ComponentModel.DataAnnotations;

namespace MARS.MediaStorage.Entities.DTOs;

public class UpdateMemeOrderDto
{
    [Required(ErrorMessage = "FilePath is required")]
    public string FilePath { get; set; } = string.Empty;

    public int? MemeTypeId { get; set; }

    [Required(ErrorMessage = "Order is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Order must be greater than 0")]
    public int Order { get; set; }
}
