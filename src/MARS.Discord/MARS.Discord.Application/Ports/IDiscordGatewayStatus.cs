using MARS.Discord.Application.Models;

namespace MARS.Discord.Application.Ports;

/// <summary>
/// Минимальная поверхность шлюза Discord, нужная API-слою.
/// </summary>
public interface IDiscordGatewayStatus
{
    bool IsConnected { get; }

    Task<OperationResult> SendMessageAsync(
        ulong channelId,
        string message,
        CancellationToken cancellationToken = default
    );
}
