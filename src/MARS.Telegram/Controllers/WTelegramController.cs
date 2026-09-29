using MARS.Telegram.Entities;
using MARS.Telegram.Services;
using Microsoft.AspNetCore.Mvc;

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
        catch (NotSupportedException)
        {
            return StatusCode(
                501,
                WTelegramOperationResult.CreateFailure(
                    "WTelegram авторизация не поддерживается в автономном микросервисе"
                )
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

    [HttpPost("verification-code")]
    public IActionResult SubmitVerificationCode(
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

            var status = clientService
                .GetClientStatusAsync(cancellationToken)
                .GetAwaiter()
                .GetResult();

            return Ok(WTelegramOperationResult.CreateSuccess("Код верификации принят", status));
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
