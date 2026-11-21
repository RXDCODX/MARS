using DSharpPlus;
using DSharpPlus.EventArgs;
using MARS.Discord.Application.Models;

namespace MARS.Discord.Infrastructure.Services.Gateway;

public interface IDiscordGatewayService
{
    bool IsConnected { get; }
    DiscordClient? Client { get; }

    void RegisterMessageCreatedHandler(Func<DiscordClient, MessageCreatedEventArgs, Task> handler);
    void RegisterVoiceStateUpdatedHandler(
        Func<DiscordClient, VoiceStateUpdatedEventArgs, Task> handler
    );
    void RegisterInteractionCreatedHandler(
        Func<DiscordClient, InteractionCreatedEventArgs, Task> handler
    );
    void RegisterComponentInteractionCreatedHandler(
        Func<DiscordClient, ComponentInteractionCreatedEventArgs, Task> handler
    );

    Task<DiscordClient?> EnsureConnectedAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> SendMessageAsync(
        ulong channelId,
        string message,
        CancellationToken cancellationToken = default
    );
}
