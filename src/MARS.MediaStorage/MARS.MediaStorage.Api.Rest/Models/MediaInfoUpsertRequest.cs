using Microsoft.AspNetCore.Http;

namespace MARS.MediaStorage.Api.Rest.Models;

public class MediaInfoUpsertRequest
{
    public required string AlertJson { get; set; }
    public IFormFile? File { get; set; }
}
