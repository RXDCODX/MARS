using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using ProtoDiscord = MARS.Shared.Contracts.Discord.Control;

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
            .CreateClient<ProtoDiscord.DiscordMessageService.DiscordMessageServiceClient>("Discord")
            .SendMessageAsync(
                new ProtoDiscord.DiscordMessageSendRequest
                {
                    ChannelId = channelId,
                    Message = message,
                },
                cancellationToken: cancellationToken
            );
    }

    public async Task SendFileAsync(
        ProtoDiscord.DiscordFileSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await clientFactory
            .CreateClient<ProtoDiscord.DiscordMessageService.DiscordMessageServiceClient>("Discord")
            .SendFileAsync(request, cancellationToken: cancellationToken);
    }
}
