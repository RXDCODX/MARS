namespace MARS.TwitchCore.Services.AutoHello;

/// <summary>
/// Interface for auto-hello functionality.
/// Implementations may depend on external microservices (e.g. WaifuRoll).
/// </summary>
public interface IAutoHelloService
{
    Task<string?> GetAutoHelloMessageAsync(string userId, string displayName);
}
