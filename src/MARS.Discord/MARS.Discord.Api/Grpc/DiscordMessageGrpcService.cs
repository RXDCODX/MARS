using System.IO;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Discord.Infrastructure.Services.Gateway;
using MARS.Platform.Infrastructure.Http.MediaArtifact;
using MARS.Shared.Contracts.Discord.Control;
using Microsoft.Extensions.Logging;

namespace MARS.Discord.Api.Grpc;

using ProtoDiscord = MARS.Shared.Contracts.Discord.Control;

public sealed class DiscordMessageGrpcService(
    IDiscordGatewayService gatewayService,
    IMediaArtifactDownloader mediaArtifactDownloader,
    ILogger<DiscordMessageGrpcService> logger
) : DiscordMessageService.DiscordMessageServiceBase
{
    private const long MaxArtifactBytes = 19L * 1024 * 1024;

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

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> SendFile(
        ProtoDiscord.DiscordFileSendRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ChannelId))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Некорректный запрос отправки файла")
            );
        }

        if (!ulong.TryParse(request.ChannelId, out var channelId))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Некорректный идентификатор канала")
            );
        }

        if (string.IsNullOrWhiteSpace(request.FileUrl))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "URL артефакта обязателен")
            );
        }

        Stream stream;
        try
        {
            stream = await mediaArtifactDownloader.DownloadAsync(
                request.FileUrl,
                MaxArtifactBytes,
                context.CancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Artifact download failed for {FileUrl}", request.FileUrl);
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Артефакт больше 19 МБ или недоступен")
            );
        }

        await using (stream)
        {
            var result = await gatewayService.SendFileAsync(
                channelId,
                stream,
                string.IsNullOrWhiteSpace(request.FileName) ? "file" : request.FileName,
                string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption,
                (ulong)Math.Max(0, request.ReplyToMessageId),
                request.MediaKind,
                context.CancellationToken
            );

            if (!result.Success)
            {
                logger.LogWarning("SendFile failed for channel {ChannelId}", request.ChannelId);
                throw new RpcException(
                    new Status(StatusCode.Internal, "Не удалось отправить файл")
                );
            }
        }

        return new Google.Protobuf.WellKnownTypes.Empty();
    }
}