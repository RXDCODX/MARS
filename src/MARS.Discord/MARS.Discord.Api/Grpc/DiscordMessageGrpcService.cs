using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Discord.Infrastructure.Services.Gateway;
using MARS.Shared.Contracts.Discord.Control;
using Microsoft.Extensions.Logging;

namespace MARS.Discord.Api.Grpc;

public sealed class DiscordMessageGrpcService(
    IDiscordGatewayService gatewayService,
    ILogger<DiscordMessageGrpcService> logger
) : DiscordMessageService.DiscordMessageServiceBase
{
    public override async Task<Google.Protobuf.WellKnownTypes.Empty> SendMessage(
        DiscordMessageSendRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Пустое сообщение"));
        }

        if (!ulong.TryParse(request.ChannelId, out var channelId))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Некорректный идентификатор канала")
            );
        }

        var result = await gatewayService.SendMessageAsync(
            channelId,
            request.Message,
            context.CancellationToken
        );
        if (!result.Success)
        {
            logger.LogWarning("SendMessage failed for channel {ChannelId}", request.ChannelId);
            throw new RpcException(
                new Status(StatusCode.Internal, "Не удалось отправить сообщение")
            );
        }

        return new Google.Protobuf.WellKnownTypes.Empty();
    }
}
