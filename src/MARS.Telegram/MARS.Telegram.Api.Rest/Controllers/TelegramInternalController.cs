using System.Net;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts.Telegram;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Logging;
using MARS.Telegram.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Api.Rest.Controllers;

/// <summary>
/// Внутренние межсервисные эндпоинты MARS.Telegram.
/// </summary>
/// <remarks>
/// <para>
/// Доступны только с корректным <c>X-Api-Key</c> (политика <c>ServiceApiKey</c>):
/// браузер сюда не ходит. Бот принадлежит этому сервису, и остальные просят
/// доставить сообщение или запись журнала его эндпоинтом, а не держат своего
/// клиента Telegram — второй клиент того же токена означал бы двух владельцев
/// прав и переменные бота в двух блоках compose.
/// </para>
/// <para>
/// Ошибки различаются статусами, а не только текстом: <c>412</c> — служба не
/// настроена (нет токена или получателей), повторять бессмысленно; <c>503</c> —
/// настроена, но доставить не удалось, повторять имеет смысл; <c>400</c> —
/// запрос неверен. Отправитель журнала по <c>412</c> отключается навсегда, и
/// именно поэтому важно не выдавать одно за другое.
/// </para>
/// </remarks>
[ApiController]
[Route("api/internal/telegram")]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
public class TelegramInternalController(
    ITelegramMessageSender sender,
    IOptions<TelegramConfiguration> telegram,
    TelegramLogRateLimiter rateLimiter,
    ILogger<TelegramInternalController> logger
) : ControllerBase
{
    /// <summary>
    /// Доставка сообщения администраторам бота.
    /// </summary>
    [HttpPost("admin-messages")]
    public async Task<ActionResult<OperationResult<TelegramAdminMessageResult>>> SendToAdmins(
        [FromBody] TelegramAdminMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var admins = telegram.Value.AdminIds;

        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
        {
            return StatusCode(
                (int)HttpStatusCode.PreconditionFailed,
                OperationResult<TelegramAdminMessageResult>.Fail(
                    "Telegram не настроен: задайте Telegram__BotToken в сервисе MARS.Telegram",
                    HttpStatusCode.PreconditionFailed
                )
            );
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(
                OperationResult<TelegramAdminMessageResult>.Fail("Пустое сообщение администраторам")
            );
        }

        if (admins.Length == 0)
        {
            return StatusCode(
                (int)HttpStatusCode.PreconditionFailed,
                OperationResult<TelegramAdminMessageResult>.Fail(
                    "Список администраторов пуст: задайте Telegram__AdminIds__0",
                    HttpStatusCode.PreconditionFailed
                )
            );
        }

        var delivered = await sender.SendToAdminsAsync(request.Message, cancellationToken);

        if (delivered == 0)
        {
            return StatusCode(
                (int)HttpStatusCode.ServiceUnavailable,
                OperationResult<TelegramAdminMessageResult>.Fail(
                    $"Не удалось доставить сообщение ни одному из {admins.Length} администраторов",
                    HttpStatusCode.ServiceUnavailable
                )
            );
        }

        logger.LogInformation(
            "Сообщение от {Service} доставлено {Delivered} из {Total} администраторов",
            string.IsNullOrWhiteSpace(request.Service) ? "неизвестного сервиса" : request.Service,
            delivered,
            admins.Length
        );

        return Ok(
            OperationResult<TelegramAdminMessageResult>.Ok(
                new TelegramAdminMessageResult { Recipients = delivered }
            )
        );
    }

    /// <summary>
    /// Приём записи журнала от соседнего сервиса.
    /// </summary>
    /// <remarks>
    /// Записи уходят обычным текстом, без разметки: текст ошибки содержит «&lt;»
    /// и прочие символы, а наш разбор разметки счёл бы их тегом и отказался бы
    /// отправлять сообщение вовсе.
    /// </remarks>
    [HttpPost("log-entries")]
    public async Task<ActionResult<OperationResult<TelegramLogEntryResult>>> PublishLogEntry(
        [FromBody] TelegramLogEntryRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Service))
        {
            return BadRequest(
                OperationResult<TelegramLogEntryResult>.Fail("Не указан сервис-отправитель")
            );
        }

        if (
            string.IsNullOrWhiteSpace(request.Message)
            && string.IsNullOrWhiteSpace(request.Exception)
        )
        {
            return BadRequest(
                OperationResult<TelegramLogEntryResult>.Fail("Пустая запись журнала")
            );
        }

        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
        {
            return StatusCode(
                (int)HttpStatusCode.PreconditionFailed,
                OperationResult<TelegramLogEntryResult>.Fail(
                    "Telegram не настроен: задайте Telegram__BotToken в сервисе MARS.Telegram",
                    HttpStatusCode.PreconditionFailed
                )
            );
        }

        var chats = Chats();

        if (chats.Length == 0)
        {
            return StatusCode(
                (int)HttpStatusCode.PreconditionFailed,
                OperationResult<TelegramLogEntryResult>.Fail(
                    "Список чатов для журнала пуст: задайте Telegram__LogChatIds__0",
                    HttpStatusCode.PreconditionFailed
                )
            );
        }

        // Лимит частоты — здесь, а не в отправителях: там видны только свои
        // записи, и суммарный поток не виден никому.
        var decision = rateLimiter.TryAcquire(request.Service, DateTimeOffset.UtcNow);

        if (!decision.Allowed)
        {
            logger.LogWarning(
                "Запись журнала от {Service} отброшена лимитом частоты",
                request.Service
            );

            return Ok(
                OperationResult<TelegramLogEntryResult>.Ok(
                    new TelegramLogEntryResult { Recipients = 0, Suppressed = decision.Suppressed }
                )
            );
        }

        var text = TelegramLogFormatter.Format(request);

        // Доставка — best effort: один недоступный чат не должен лишать
        // остальные записи. Никто не доставил — это уже ошибка доставки, и
        // вызывающий должен узнать о ней статусом, а не нулевым числом молча.
        var delivered = await sender.SendPlainToChatsAsync(chats, text, cancellationToken);

        if (delivered == 0)
        {
            return StatusCode(
                (int)HttpStatusCode.ServiceUnavailable,
                OperationResult<TelegramLogEntryResult>.Fail(
                    $"Не удалось доставить запись ни одному из {chats.Length} чатов",
                    HttpStatusCode.ServiceUnavailable
                )
            );
        }

        logger.LogInformation(
            "Запись журнала от {Service} доставлена {Delivered} из {Total} чатов",
            request.Service,
            delivered,
            chats.Length
        );

        return Ok(
            OperationResult<TelegramLogEntryResult>.Ok(
                new TelegramLogEntryResult
                {
                    Recipients = delivered,
                    Suppressed = decision.Suppressed,
                }
            )
        );
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
