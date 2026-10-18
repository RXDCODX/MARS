using Grpc.Core;
using MARS.Shared.Grpc.TelegramAdmin;
using MARS.Telegram.Configuration;
using MARS.Telegram.Services.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
/// Отправка идёт через <see cref="ITelegramMessageSender"/>, а не через клиента
/// Bot API: тот недоступен с машины владельца, и вызов уходил бы в таймаут. Тот же
/// отправитель использует и буфер обмена, поэтому «владелец бота» в сервисе один.
/// </para>
/// </remarks>
public sealed class TelegramAdminGrpcService(
    ITelegramMessageSender sender,
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

        // Токен проверяется здесь, а не внутри отправителя: отказ «Telegram не
        // настроен» — это состояние службы (FAILED_PRECONDITION), а не ошибка
        // доставки, и вызывающий обязан различать их.
        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
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

        // Доставка — best effort: один недоступный администратор не должен лишать
        // остальных сообщения. Никто не доставил — это уже ошибка доставки, и
        // вызывающий должен узнать о ней статусом, а не нулевым числом молча.
        var delivered = await sender.SendToAdminsAsync(message, context.CancellationToken);

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
}
