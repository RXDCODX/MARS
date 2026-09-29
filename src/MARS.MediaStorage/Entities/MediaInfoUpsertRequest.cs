using Microsoft.AspNetCore.Http;

namespace MARS.MediaStorage.Entities;

public class MediaInfoUpsertRequest
{
    public required string AlertJson { get; set; }
    public IFormFile? File { get; set; }
}
