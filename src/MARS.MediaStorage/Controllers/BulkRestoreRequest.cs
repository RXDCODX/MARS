using MARS.MediaStorage.Entities.DTOs;
using MARS.MediaStorage.Services.Media;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Controllers;

public class BulkRestoreRequest
{
    public required List<Guid> Ids { get; set; }

    public bool DryRun { get; set; }
}
