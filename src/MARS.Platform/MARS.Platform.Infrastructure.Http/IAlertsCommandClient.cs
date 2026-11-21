using MARS.Shared.Contracts.Alerts;

<<<<<<< HEAD:src/MARS.Platform/MARS.Platform.Infrastructure.Http/IAlertsCommandClient.cs
namespace MARS.Platform.Infrastructure.Http;
=======
namespace MARS.Platform.Infrastructure.Clients.Http;
>>>>>>> origin/main:src/MARS.Platform/MARS.Platform.Infrastructure.Clients.Http/IAlertsCommandClient.cs

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
