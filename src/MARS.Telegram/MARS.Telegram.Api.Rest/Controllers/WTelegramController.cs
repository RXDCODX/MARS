using System.Net;
using MARS.Shared.Kernel;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Exceptions;
using MARS.Telegram.Application.Models;
using MARS.Telegram.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Api.Rest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WTelegramController(
    IWTelegramClientService clientService,
    ITelegramMessageSender sender,
    TelegramInboxStore inbox,
    ITelegramBotStatusService botStatus,
    IOptions<TelegramConfiguration> telegram,
    ILogger<WTelegramController> logger
) : ControllerBase
{
    [HttpPost("relogin")]
    public async Task<ActionResult<WTelegramOperationResult>> ReLogin(
        CancellationToken cancellationToken
    )
    {
        try
        {
            logger.LogInformation("Запрос на принудительную переавторизацию WTelegram");

            await clientService.ReLoginAsync(cancellationToken);

            var status = await clientService.GetClientStatusAsync(cancellationToken);

            return Ok(
                WTelegramOperationResult.CreateSuccess("Переавторизация выполнена успешно", status)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при переавторизации WTelegram");

            return StatusCode(
                500,
                WTelegramOperationResult.CreateFailure("Ошибка при переавторизации", ex.Message)
            );
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<WTelegramOperationResult>> GetStatus(
        CancellationToken cancellationToken
    )
    {
        try
        {
            var status = await clientService.GetClientStatusAsync(cancellationToken);

            return Ok(status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении статуса WTelegram");

            return StatusCode(
                500,
                WTelegramOperationResult.CreateFailure("Ошибка при получении статуса", ex.Message)
            );
        }
    }

    /// <summary>
    /// Приём кода подтверждения входа.
    /// </summary>
    /// <remarks>
    /// Принятый код сам по себе ничего не завершает: клиент читает его при
    /// попытке входа, поэтому после приёма контроллер сразу её инициирует. Иначе
    /// администратор ввёл бы код и ждал результата, которого не наступило бы до
    /// нажатия «Переавторизация».
    /// </remarks>
    [HttpPost("verification-code")]
    public async Task<ActionResult<WTelegramOperationResult>> SubmitVerificationCode(
        [FromBody] VerificationCodeRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            logger.LogInformation("Получен запрос на отправку кода верификации");

            var accepted = clientService.SubmitVerificationCode(request.Code);

            if (!accepted)
            {
                return BadRequest(
                    WTelegramOperationResult.CreateFailure(
                        "Код верификации не ожидается в данный момент"
                    )
                );
            }

            await clientService.ReLoginAsync(cancellationToken);

            var status = await clientService.GetClientStatusAsync(cancellationToken);

            return Ok(WTelegramOperationResult.CreateSuccess("Код верификации принят", status));
        }
        catch (TelegramVerificationCodeRejectedException ex)
        {
            // Ответ Telegram на введённый код — это ответ на ввод, а не поломка
            // службы: клиент показывает причину и ждёт нового кода. Страница
            // показывает сообщение, поэтому оно должно быть точным.
            logger.LogWarning(ex, "Telegram не принял код подтверждения");

            return StatusCode(
                StatusCodes.Status400BadRequest,
                WTelegramOperationResult.CreateFailure(
                    "Telegram не принял код подтверждения",
                    ex.Message
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отправке кода верификации");

            return StatusCode(
                500,
                WTelegramOperationResult.CreateFailure(
                    "Ошибка при отправке кода верификации",
                    ex.Message
                )
            );
        }
    }

    /// <summary>
    /// Снимок недавно полученных ботом сообщений (для страницы консоли).
    /// </summary>
    [HttpGet("inbox")]
    public ActionResult<IReadOnlyList<TelegramInboxEntry>> GetInbox() => Ok(inbox.Snapshot());

    /// <summary>
    /// Пассивный статус MTProto-подключения бота.
    /// </summary>
    [HttpGet("bot-status")]
    public ActionResult<TelegramBotStatus> GetBotStatus() => Ok(botStatus.GetStatus());

    /// <summary>
    /// Активная проверка подключения бота: при необходимости инициирует вход.
    /// </summary>
    [HttpPost("bot-check")]
    public async Task<ActionResult<TelegramBotStatus>> CheckBotConnection(
        CancellationToken cancellationToken = default
    ) => Ok(await botStatus.CheckAsync(cancellationToken));

    /// <summary>
    /// Отправка сообщения администраторам из страницы консоли.
    /// </summary>
    [HttpPost("send-message")]
    public async Task<ActionResult<OperationResult<int>>> SendMessageToAdmins(
        [FromBody] TelegramConsoleSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(OperationResult.Fail("Пустое сообщение"));
        }

        var admins = telegram.Value.AdminIds;

        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
        {
            return StatusCode(
                (int)HttpStatusCode.PreconditionFailed,
                OperationResult.Fail("Telegram не настроен: задайте Telegram__BotToken")
            );
        }

        if (admins.Length == 0)
        {
            return StatusCode(
                (int)HttpStatusCode.PreconditionFailed,
                OperationResult.Fail("Список администраторов пуст: задайте Telegram__AdminIds__0")
            );
        }

        var delivered = await sender.SendToAdminsAsync(request.Text, cancellationToken);

        if (delivered == 0)
        {
            return StatusCode(
                (int)HttpStatusCode.ServiceUnavailable,
                OperationResult.Fail("Не удалось отправить сообщение ни одному администратору")
            );
        }

        return Ok(OperationResult<int>.Ok(delivered));
    }
}
