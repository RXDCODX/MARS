using Grpc.Core;
using MARS.Shared.Contracts.TelegramAdmin;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Services;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Api.Rest.Grpc;

public sealed class TelegramAdminGrpcService(
    ITelegramMessageSender sender,
    IOptions<TelegramConfiguration> telegram,
    ILogger<TelegramAdminGrpcService> logger
) : TelegramAdminService.TelegramAdminServiceBase
{
    public override async Task<AdminMessageReply> SendToAdmins(
        AdminMessageRequest request,
        ServerCallContext context
    )
    {
        var admins = telegram.Value.AdminIds;

        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "Telegram не настроен: задайте Telegram__BotToken"
                )
            );
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Пустое сообщение администраторам")
            );
        }

        if (admins.Length == 0)
        {
            throw new RpcException(
                new Status(StatusCode.FailedPrecondition, "Список администраторов пуст")
            );
        }

        var delivered = await sender.SendToAdminsAsync(request.Message, context.CancellationToken);

        if (delivered == 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.Unavailable,
                    "Не удалось доставить сообщение ни одному администратору"
                )
            );
        }

        logger.LogInformation(
            "Сообщение от {Service} доставлено {Delivered} из {Total} администраторов",
            string.IsNullOrWhiteSpace(request.Service) ? "неизвестного сервиса" : request.Service,
            delivered,
            admins.Length
        );

        return new AdminMessageReply { Recipients = delivered };
    }
}
