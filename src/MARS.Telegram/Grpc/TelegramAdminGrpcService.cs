using Grpc.Core;
using MARS.Shared.Grpc.TelegramAdmin;
using MARS.Telegram.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace MARS.Telegram.Grpc;

/// <summary>
/// Доставка сообщения администраторам от сервиса, которому нужен владелец бота.
/// </summary>
/// <remarks>
/// <para>
/// Бот принадлежит этому сервису: остальные (сейчас <c>MARS.TwitchCore</c> с
/// просьбой пройти переавторизацию Twitch) обращаются сюда по gRPC, а не держат
/// своего клиента. Второй клиент того же токена означал бы двух владельцев прав и
/// переменные бота в двух блоках compose.
/// </para>
/// <para>
/// Клиент бота внедряется как <c>ITelegramBotClient?</c> и регистрируется в DI
/// только при непустом токене (см. <c>Program.cs</c>), поэтому запрос обхода
/// контейнера — единственный способ честно сказать «Telegram не настроен», а не
/// упасть с <c>Unable to resolve service</c> у вызывающего.
/// </para>
/// </remarks>
public sealed class TelegramAdminGrpcService(
    ITelegramBotClient? client,
    IOptions<TelegramConfiguration> telegram,
    ILogger<TelegramAdminGrpcService> logger
) : TelegramAdminService.TelegramAdminServiceBase
{
    /// <inheritdoc />
    public override async Task<AdminMessageReply> SendToAdmins(
        AdminMessageRequest request,
        ServerCallContext context
    )
    {
        var service = request?.Service ?? string.Empty;
        var message = request?.Message ?? string.Empty;
        var admins = telegram.Value.AdminIds;

        if (client is null)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "Telegram не настроен: задайте Telegram__BotToken в сервисе MARS.Telegram"
                )
            );
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Пустое сообщение администраторам")
            );
        }

        if (admins.Length == 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "Список администраторов пуст: задайте Telegram__AdminIds__0"
                )
            );
        }

        var delivered = 0;

        // Доставка — best effort: один недоступный администратор не должен лишать
        // остальных сообщения. Никто не доставил — это уже ошибка доставки, и
        // вызывающий должен узнать о ней статусом, а не нулевым числом молча.
        foreach (var adminId in admins)
        {
            if (await TrySendAsync(client, adminId, message, context))
            {
                delivered++;
            }
        }

        if (delivered == 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.Unavailable,
                    $"Не удалось доставить сообщение ни одному из {admins.Length} администраторов"
                )
            );
        }

        logger.LogInformation(
            "Сообщение от {Service} доставлено {Delivered} из {Total} администраторов",
            string.IsNullOrWhiteSpace(service) ? "неизвестного сервиса" : service,
            delivered,
            admins.Length
        );

        return new AdminMessageReply { Recipients = delivered };
    }

    private async Task<bool> TrySendAsync(
        ITelegramBotClient botClient,
        long adminId,
        string message,
        ServerCallContext context
    )
    {
        var result = false;

        try
        {
            await botClient.SendMessage(
                adminId,
                message,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken
            );

            result = true;
        }
        catch (ApiRequestException ex)
        {
            logger.LogWarning(
                ex,
                "Telegram отклонил сообщение администратору {AdminId}: {Message}",
                adminId,
                ex.Message
            );
        }

        return result;
    }
}
