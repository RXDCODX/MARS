using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Entities.DTOs;

public class IndexResultDto
{
    public int Added { get; set; }

    public int Updated { get; set; }

    public IndexResultDto() { }

    public IndexResultDto(IndexResult result)
    {
        Added = result.Added;
        Updated = result.Updated;
    }
}
