using MARS.Shared.Contracts.Models;

namespace MARS.TTS.Services;

public interface ITtsMessageFilterService
{
    bool IsFilterEnabled { get; set; }

    OperationResult<string> FilterMessage(string message, string? userId = null);

    Task LoadStateAsync(CancellationToken cancellationToken = default);
}
