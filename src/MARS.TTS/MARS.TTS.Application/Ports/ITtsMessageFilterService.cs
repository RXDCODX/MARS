using MARS.Shared.Contracts.Models;

namespace MARS.TTS.Application.Ports;

public interface ITtsMessageFilterService
{
    bool IsFilterEnabled { get; set; }

    OperationResult<string> FilterMessage(string message, string? userId = null);

    Task LoadStateAsync(CancellationToken cancellationToken = default);
}
