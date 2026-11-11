using System.ComponentModel.DataAnnotations;

namespace MARS.MediaStorage.Api.Rest.Models;

public class MemeOrderDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }

    [Required(ErrorMessage = "FilePath is required")]
    public string FilePath { get; set; } = string.Empty;

    public int? MemeTypeId { get; set; }
    public MemeTypeDto? Type { get; set; }
}
