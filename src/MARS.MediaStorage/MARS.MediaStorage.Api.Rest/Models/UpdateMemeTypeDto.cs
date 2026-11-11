using System.ComponentModel.DataAnnotations;

namespace MARS.MediaStorage.Api.Rest.Models;

public class UpdateMemeTypeDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "FolderPath is required")]
    public string FolderPath { get; set; } = string.Empty;
}
