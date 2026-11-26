using Grpc.Core;
using MARS.Shared.Contracts.Telegram;
using MARS.Shared.Contracts.TelegramLog;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Logging;
using MARS.Telegram.Application.Services;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Api.Rest.Grpc;

public sealed class TelegramLogGrpcService(
    ITelegramMessageSender sender,
    IOptions<TelegramConfiguration> telegram,
    TelegramLogRateLimiter rateLimiter,
    ILogger<TelegramLogGrpcService> logger
) : TelegramLogService.TelegramLogServiceBase
{
    public override async Task<LogEntryReply> Publish(
        LogEntryRequest request,
        ServerCallContext context
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Service))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Не указан сервис-отправитель"));
        }

        if (string.IsNullOrWhiteSpace(request.Message) && string.IsNullOrWhiteSpace(request.Exception))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Пустая запись журнала"));
        }

        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Telegram не настроен"));
        }

        var chats = Chats();
        if (chats.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Список чатов для журнала пуст"));
        }

        var decision = rateLimiter.TryAcquire(request.Service, DateTimeOffset.UtcNow);
        if (!decision.Allowed)
        {
            logger.LogWarning("Запись журнала от {Service} отброшена лимитом частоты", request.Service);
            return new LogEntryReply { Recipients = 0, Suppressed = decision.Suppressed };
        }

        var legacyRequest = new TelegramLogEntryRequest
        {
            Service = request.Service,
            Level = request.Level,
            Category = request.Category,
            Message = request.Message,
            Exception = request.Exception,
            Timestamp = request.Timestamp.ToDateTimeOffset(),
            SuppressedCount = request.SuppressedCount,
        };
        var text = TelegramLogFormatter.Format(legacyRequest);
        var delivered = await sender.SendPlainToChatsAsync(chats, text, context.CancellationToken);

        if (delivered == 0)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Не удалось доставить запись ни одному чату"));
        }

        logger.LogInformation(
            "Запись журнала от {Service} доставлена {Delivered} из {Total} чатов",
            request.Service,
            delivered,
            chats.Length
        );

        return new LogEntryReply { Recipients = delivered, Suppressed = decision.Suppressed };
    }

    private long[] Chats()
    {
        var result = telegram.Value.LogChatIds;
        if (result.Length == 0)
        {
            result = telegram.Value.AdminIds;
        }

        return result;
    }
}