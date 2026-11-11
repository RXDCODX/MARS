using MARS.MediaStorage.Application.Storage;
using MARS.MediaStorage.Domain.Entities;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Api.Rest.Models;

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
