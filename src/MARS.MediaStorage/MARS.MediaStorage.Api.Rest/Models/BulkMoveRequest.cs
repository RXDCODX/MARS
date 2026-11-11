using MARS.MediaStorage.Application.Media;
using MARS.MediaStorage.Application.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Api.Rest.Models;

public class BulkMoveRequest : BulkStorageRequest
{
    public required string TargetDirectory { get; set; }
}
