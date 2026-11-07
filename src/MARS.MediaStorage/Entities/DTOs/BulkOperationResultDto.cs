using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.MediaStorage.Entities.DTOs;

public class BulkOperationResultDto
{
    public int Requested { get; set; }

    public int Succeeded { get; set; }

    public int Failed { get; set; }

    public List<string> Errors { get; set; } = [];

    public BulkOperationResultDto() { }

    public BulkOperationResultDto(BulkOperationResult result)
    {
        Requested = result.Requested;
        Succeeded = result.Succeeded;
        Failed = result.Failed;
        Errors = [.. result.Errors];
    }
}
