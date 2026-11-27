using System;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MARS.Shared.Contracts.TelegramMessage;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Api.Rest.Grpc;

public sealed class TelegramMessageGrpcService(
    ITelegramMessageSender sender,
    ITelegramMediaSender mediaSender,
    IGooglePhotosAuthService googlePhotosAuthService,
    IRandomMemService randomMemService,
    IWTelegramClientService wTelegramClientService,
    ITelegramWhitelistService whitelistService,
    IOptions<TelegramConfiguration> telegram,
    ILogger<TelegramMessageGrpcService> logger
) : TelegramMessageService.TelegramMessageServiceBase
{
    public TelegramMessageGrpcService(
        ITelegramMessageSender sender,
        IGooglePhotosAuthService googlePhotosAuthService,
        IRandomMemService randomMemService,
        IWTelegramClientService wTelegramClientService,
        ITelegramWhitelistService whitelistService,
        IOptions<TelegramConfiguration> telegram,
        ILogger<TelegramMessageGrpcService> logger
    )
        : this(
            sender,
            null!,
            googlePhotosAuthService,
            randomMemService,
            wTelegramClientService,
            whitelistService,
            telegram,
            logger
        ) { }

    public TelegramMessageGrpcService(
        ITelegramMediaSender mediaSender,
        ITelegramMessageSender sender,
        IGooglePhotosAuthService googlePhotosAuthService,
        IRandomMemService randomMemService,
        IWTelegramClientService wTelegramClientService,
        ITelegramWhitelistService whitelistService,
        IOptions<TelegramConfiguration> telegram,
        ILogger<TelegramMessageGrpcService> logger
    )
        : this(
            sender,
            mediaSender,
            googlePhotosAuthService,
            randomMemService,
            wTelegramClientService,
            whitelistService,
            telegram,
            logger
        ) { }

    public TelegramMessageGrpcService(
        ITelegramMessageSender sender,
        IGooglePhotosAuthService googlePhotosAuthService,
        ITelegramMediaSender mediaSender,
        IRandomMemService randomMemService,
        IWTelegramClientService wTelegramClientService,
        ITelegramWhitelistService whitelistService,
        IOptions<TelegramConfiguration> telegram,
        ILogger<TelegramMessageGrpcService> logger
    )
        : this(
            sender,
            mediaSender,
            googlePhotosAuthService,
            randomMemService,
            wTelegramClientService,
            whitelistService,
            telegram,
            logger
        ) { }

    public TelegramMessageGrpcService(
        ITelegramMessageSender sender,
        IGooglePhotosAuthService googlePhotosAuthService,
        IRandomMemService randomMemService,
        ITelegramMediaSender mediaSender,
        IWTelegramClientService wTelegramClientService,
        ITelegramWhitelistService whitelistService,
        IOptions<TelegramConfiguration> telegram,
        ILogger<TelegramMessageGrpcService> logger
    )
        : this(
            sender,
            mediaSender,
            googlePhotosAuthService,
            randomMemService,
            wTelegramClientService,
            whitelistService,
            telegram,
            logger
        ) { }

    public TelegramMessageGrpcService(
        ITelegramMessageSender sender,
        IGooglePhotosAuthService googlePhotosAuthService,
        IRandomMemService randomMemService,
        IWTelegramClientService wTelegramClientService,
        ITelegramMediaSender mediaSender,
        ITelegramWhitelistService whitelistService,
        IOptions<TelegramConfiguration> telegram,
        ILogger<TelegramMessageGrpcService> logger
    )
        : this(
            sender,
            mediaSender,
            googlePhotosAuthService,
            randomMemService,
            wTelegramClientService,
            whitelistService,
            telegram,
            logger
        ) { }

    public TelegramMessageGrpcService(
        ITelegramMessageSender sender,
        IGooglePhotosAuthService googlePhotosAuthService,
        IRandomMemService randomMemService,
        IWTelegramClientService wTelegramClientService,
        ITelegramWhitelistService whitelistService,
        ITelegramMediaSender mediaSender,
        IOptions<TelegramConfiguration> telegram,
        ILogger<TelegramMessageGrpcService> logger
    )
        : this(
            sender,
            mediaSender,
            googlePhotosAuthService,
            randomMemService,
            wTelegramClientService,
            whitelistService,
            telegram,
            logger
        ) { }

    public TelegramMessageGrpcService(
        ITelegramMessageSender sender,
        IGooglePhotosAuthService googlePhotosAuthService,
        IRandomMemService randomMemService,
        IWTelegramClientService wTelegramClientService,
        ITelegramWhitelistService whitelistService,
        IOptions<TelegramConfiguration> telegram,
        ITelegramMediaSender mediaSender,
        ILogger<TelegramMessageGrpcService> logger
    )
        : this(
            sender,
            mediaSender,
            googlePhotosAuthService,
            randomMemService,
            wTelegramClientService,
            whitelistService,
            telegram,
            logger
        ) { }

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

    public override async Task<SendMessageReply> SendMedia(
        SendMediaRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.FileUrl))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Пустое медиа"));
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

        var delivered = await mediaSender.SendMediaAsync(
            request.ChatId,
            request.FileUrl,
            request.FileName,
            request.MimeType,
            request.Caption,
            request.ReplyToMessageId,
            request.MediaKind,
            request.DurationSeconds,
            request.Width,
            request.Height,
            context.CancellationToken
        );
        if (!delivered)
        {
            throw new RpcException(
                new Status(StatusCode.Unavailable, "Не удалось отправить медиа в Telegram")
            );
        }

        logger.LogInformation("Медиа доставлено в Telegram чат {ChatId}", request.ChatId);

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

    public override Task<Empty> SetRandomMemEnabled(
        RandomMemEnabledRequest request,
        ServerCallContext context
    )
    {
        _ = randomMemService.SetEnabledAsync(request?.Enabled ?? false, context.CancellationToken);
        return Task.FromResult(new Empty());
    }

    public override async Task<WhitelistReply> GetWhitelist(
        Empty request,
        ServerCallContext context
    )
    {
        var users = await whitelistService.GetPyroAlertsUsersAsync(context.CancellationToken);
        var reply = new WhitelistReply();
        reply.Users.AddRange(users);
        return reply;
    }

    public override async Task<WTelegramStatusReply> GetWTelegramStatus(
        Empty request,
        ServerCallContext context
    )
    {
        var status = await wTelegramClientService.GetClientStatusAsync(context.CancellationToken);
        var text =
            $"Authenticated: {status.IsAuthenticated}\\n"
            + $"UserId: {status.UserId}\\n"
            + $"Username: {status.Username}\\n"
            + $"Phone: {status.Phone}\\n"
            + $"AwaitingCode: {status.IsAwaitingCode}\\n"
            + $"Error: {status.ErrorMessage}";
        return new WTelegramStatusReply { Status = text };
    }
}
