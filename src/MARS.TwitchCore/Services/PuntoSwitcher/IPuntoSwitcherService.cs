using MARS.Shared.Contracts.Models;

namespace MARS.TwitchCore.Services.PuntoSwitcher;

public interface IPuntoSwitcherService
{
    bool IsFilterEnabled { get; set; }
    OperationResult<PuntoSwitchSuggestion> TryFixMessage(string? message);
}
