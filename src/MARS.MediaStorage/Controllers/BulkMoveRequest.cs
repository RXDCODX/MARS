using MARS.MediaStorage.Entities.DTOs;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Controllers;

public class BulkMoveRequest : BulkStorageRequest
{
    public required string TargetDirectory { get; set; }
}
