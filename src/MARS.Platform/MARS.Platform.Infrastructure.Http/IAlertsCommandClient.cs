using MARS.Shared.Contracts.Alerts;

namespace MARS.Platform.Infrastructure.Http;

/// <summary>
/// Вызов действий обработчиков алертов из команд (MARS.Alerts, REST, internal).
/// </summary>
public interface IAlertsCommandClient
{
    Task<string> InvokeAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    );
}
