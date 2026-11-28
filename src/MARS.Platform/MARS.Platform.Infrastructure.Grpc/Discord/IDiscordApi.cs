using Google.Protobuf.WellKnownTypes;
using ProtoDiscord = MARS.Shared.Contracts.Discord.Control;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Platform.Infrastructure.Grpc.Discord;

public interface IDiscordApi
{
    Task SendMessageAsync(
        string channelId,
        string message,
        CancellationToken cancellationToken = default
    );

    Task SendFileAsync(
        ProtoDiscord.DiscordFileSendRequest request,
        CancellationToken cancellationToken = default
    );
}
