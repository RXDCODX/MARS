using Grpc.Core;
using MARS.Shared.Contracts.TelegramMessage;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Services;
using MARS.Telegram.Infrastructure.GooglePhotos.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Api.Rest.Grpc;

public sealed class TelegramMessageGrpcService(
    ITelegramMessageSender sender,
    IGooglePhotosAuthService googlePhotosAuthService,
    IOptions<TelegramConfiguration> telegram,
    ILogger<TelegramMessageGrpcService> logger
) : TelegramMessageService.TelegramMessageServiceBase
{
    public override async Task<SendMessageReply> Send(
        SendMessageRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Пустое сообщение"));
        }

        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "Telegram не настроен: задайте Telegram__BotToken"
                )
            );
        }

        var delivered = await sender.SendAsync(
            request.ChatId,
            request.Text,
            context.CancellationToken
        );
        if (!delivered)
        {
            throw new RpcException(
                new Status(StatusCode.Unavailable, "Не удалось отправить сообщение в Telegram")
            );
        }

        logger.LogInformation("Сообщение доставлено в Telegram чат {ChatId}", request.ChatId);

        return new SendMessageReply { Delivered = true };
    }

    public override async Task<GooglePhotosAuthorizationUrlReply> GetGooglePhotosAuthorizationUrl(
        Empty request,
        ServerCallContext context
    )
    {
        var url = await googlePhotosAuthService.GetAuthorizationUrlAsync(context.CancellationToken);
        return new GooglePhotosAuthorizationUrlReply { Url = url };
    }
}
