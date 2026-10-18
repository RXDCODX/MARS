using MARS.Telegram.Entities;
using MARS.Telegram.Services;
using Microsoft.AspNetCore.Mvc;
using WTelegram;

namespace MARS.Telegram.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WTelegramController(
    IWTelegramClientService clientService,
    ILogger<WTelegramController> logger
) : ControllerBase
{
    [HttpPost("relogin")]
    public async Task<IActionResult> ReLogin(CancellationToken cancellationToken)
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
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
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
    public async Task<IActionResult> SubmitVerificationCode(
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
        catch (Exception ex) when (ex is WTException or TL.RpcException)
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
}
