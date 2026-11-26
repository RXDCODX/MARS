using MARS.Shared.Contracts.Discord.Control;
using Google.Protobuf.WellKnownTypes;

namespace MARS.Platform.Infrastructure.Grpc.Discord;

public interface IDiscordApi
{
    Task SendMessageAsync(string channelId, string message, CancellationToken cancellationToken = default);
}