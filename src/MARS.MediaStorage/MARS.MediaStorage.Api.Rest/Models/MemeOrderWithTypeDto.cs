using System.ComponentModel.DataAnnotations;

namespace MARS.MediaStorage.Api.Rest.Models;

public class MemeOrderWithTypeDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int? MemeTypeId { get; set; }
    public string? TypeName { get; set; }
    public string? TypeFolderPath { get; set; }
}
