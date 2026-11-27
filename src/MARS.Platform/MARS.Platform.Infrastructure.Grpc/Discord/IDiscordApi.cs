using Google.Protobuf.WellKnownTypes;
using ProtoDiscord = MARS.Shared.Contracts.Discord.Control;

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
