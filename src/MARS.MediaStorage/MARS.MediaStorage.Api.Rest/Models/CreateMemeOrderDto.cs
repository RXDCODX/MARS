using System.ComponentModel.DataAnnotations;

namespace MARS.MediaStorage.Api.Rest.Models;

public class CreateMemeOrderDto
{
    [Required(ErrorMessage = "FilePath is required")]
    public string FilePath { get; set; } = string.Empty;

    public int? MemeTypeId { get; set; }
}
