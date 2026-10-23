using Grpc.Core;
using MARS.Shared.Grpc.TelegramLog;
using MARS.Telegram.Configuration;
using MARS.Telegram.Services.Logging;
using MARS.Telegram.Services.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Grpc;

/// <summary>
/// Приём записей журнала от соседних сервисов.
/// </summary>
/// <remarks>
/// <para>
/// Сборщик записей живёт в каждом сервисе (MARS.Shared), а доставка — здесь:
/// бот принадлежит этому сервису, и второй клиент того же токена означал бы
/// двух владельцев прав и переменные бота в двух блоках compose.
/// </para>
/// <para>
/// Записи уходят обычным текстом, без разметки: текст ошибки содержит «&lt;» и
/// прочие символы, а наш разбор разметки счёл бы их тегом и отказался бы
/// отправлять сообщение вовсе.
/// </para>
/// <para>
/// Ошибки несут стандартные статусы gRPC: <c>FAILED_PRECONDITION</c> — Telegram
/// не настроен получать журнал, <c>INVALID_ARGUMENT</c> — пустая запись,
/// <c>UNAVAILABLE</c> — доставить не удалось. Собственное поле с ошибкой рядом
/// со стандартным статусом означало бы вторую систему ошибок.
/// </para>
/// </remarks>
public sealed class TelegramLogGrpcService(
    ITelegramMessageSender sender,
    IOptions<TelegramConfiguration> telegram,
    TelegramLogRateLimiter rateLimiter,
    ILogger<TelegramLogGrpcService> logger
) : TelegramLogService.TelegramLogServiceBase
{
    /// <inheritdoc />
    public override async Task<LogEntryReply> Publish(
        LogEntryRequest request,
        ServerCallContext context
    )
    {
        var service = request?.Service ?? string.Empty;
        var message = request?.Message ?? string.Empty;
        var exception = request?.Exception ?? string.Empty;

        if (string.IsNullOrWhiteSpace(service))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "Не указан сервис-отправитель")
            );
        }

        if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(exception))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Пустая запись журнала"));
        }

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

        var chats = Chats();

        if (chats.Length == 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "Список чатов для журнала пуст: задайте Telegram__LogChatIds__0"
                )
            );
        }

        // Лимит частоты — здесь, а не в отправителях: там видны только свои
        // записи, и суммарный поток не виден никому.
        var decision = rateLimiter.TryAcquire(service, DateTimeOffset.UtcNow);

        if (!decision.Allowed)
        {
            logger.LogWarning("Запись журнала от {Service} отброшена лимитом частоты", service);

            return new LogEntryReply { Recipients = 0, Suppressed = decision.Suppressed };
        }

        var text = TelegramLogFormatter.Format(request);

        // Доставка — best effort: один недоступный чат не должен лишать
        // остальные записи. Никто не доставил — это уже ошибка доставки, и
        // вызывающий должен узнать о ней статусом, а не нулевым числом молча.
        var delivered = await sender.SendPlainToChatsAsync(chats, text, context.CancellationToken);

        if (delivered == 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.Unavailable,
                    $"Не удалось доставить запись ни одному из {chats.Length} чатов"
                )
            );
        }

        logger.LogInformation(
            "Запись журнала от {Service} доставлена {Delivered} из {Total} чатов",
            service,
            delivered,
            chats.Length
        );

        return new LogEntryReply { Recipients = delivered, Suppressed = decision.Suppressed };
    }

    /// <summary>
    /// Чаты для журнала: отдельный список, а если он пуст — администраторы.
    /// </summary>
    /// <remarks>
    /// Откат на администраторов лучше, чем отсутствие журнала вовсе: отдельный
    /// чат завести забывают чаще, чем он нужен.
    /// </remarks>
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
