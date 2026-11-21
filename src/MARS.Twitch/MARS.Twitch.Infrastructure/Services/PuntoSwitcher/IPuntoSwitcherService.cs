using MARS.Shared.Contracts.Models;

namespace MARS.Twitch.Infrastructure.Services.PuntoSwitcher;

public interface IPuntoSwitcherService
{
    bool IsFilterEnabled { get; set; }
    OperationResult<PuntoSwitchSuggestion> TryFixMessage(string? message);
}
