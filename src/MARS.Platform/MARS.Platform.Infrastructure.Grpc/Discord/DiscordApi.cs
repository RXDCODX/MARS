using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using MARS.Shared.Contracts.Discord.Control;

namespace MARS.Platform.Infrastructure.Grpc.Discord;

internal sealed class DiscordApi(GrpcClientFactory clientFactory) : IDiscordApi
{
    public async Task SendMessageAsync(
        string channelId,
        string message,
        CancellationToken cancellationToken = default
    )
    {
        await clientFactory
            .CreateClient<DiscordMessageService.DiscordMessageServiceClient>("Discord")
            .SendMessageAsync(
                new DiscordMessageSendRequest { ChannelId = channelId, Message = message },
                cancellationToken: cancellationToken
            );
    }
}
