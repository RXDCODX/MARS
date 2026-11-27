using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegram;
using MARS.Shared.Kernel;
using MARS.Telegram.Application.Configuration;
using MARS.Telegram.Application.Logging;
using MARS.Telegram.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MARS.Telegram.Api.Rest.Controllers;

/// <summary>
/// Внешний webhook Alertmanager. Это единственный оставшийся REST-вход
/// MARS.Telegram: межсервисные сообщения и журнал переведены на gRPC.
/// </summary>
[ApiController]
[Route("api/internal/telegram")]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
public class TelegramAlertController(
    ITelegramMessageSender sender,
    IOptions<TelegramConfiguration> telegram,
    ILogger<TelegramAlertController> logger
) : ControllerBase
{
    /// <summary>
    /// Приём алерта от Alertmanager.
    /// </summary>
    /// <remarks>
    /// Алерты — это редкие, но важные события, поэтому здесь нет лимита частоты
    /// журнала: душить алерт из-за потока обычных записей нельзя. 503 при
    /// неудачной доставке заставляет Alertmanager повторить попытку.
    /// </remarks>
    [HttpPost("alerts")]
    public async Task<ActionResult<OperationResult<TelegramAlertResult>>> PublishAlert(
        [FromBody] TelegramAlertWebhookRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || request.Alerts.Count == 0)
        {
            return BadRequest(OperationResult<TelegramAlertResult>.Fail("Пустой алерт"));
        }

        if (string.IsNullOrWhiteSpace(telegram.Value.BotToken))
        {
            return StatusCode(
                412,
                OperationResult<TelegramAlertResult>.Fail(
                    "Telegram не настроен: задайте Telegram__BotToken в сервисе MARS.Telegram"
                )
            );
        }

        var chats = Chats();
        if (chats.Length == 0)
        {
            return StatusCode(
                412,
                OperationResult<TelegramAlertResult>.Fail(
                    "Список чатов для журнала пуст: задайте Telegram__LogChatIds__0"
                )
            );
        }

        var text = TelegramAlertFormatter.Format(request);
        var delivered = await sender.SendPlainToChatsAsync(chats, text, cancellationToken);

        if (delivered == 0)
        {
            return StatusCode(
                503,
                OperationResult<TelegramAlertResult>.Fail(
                    $"Не удалось доставить алерт ни одному из {chats.Length} чатов"
                )
            );
        }

        logger.LogInformation(
            "Алерт {AlertName} ({Status}) доставлен {Delivered} из {Total} чатов",
            request.Alerts[0].Labels.GetValueOrDefault("alertname", "alert"),
            request.Status,
            delivered,
            chats.Length
        );

        return Ok(
            OperationResult<TelegramAlertResult>.Ok(
                new TelegramAlertResult { Recipients = delivered }
            )
        );
    }

    private long[] Chats()
    {
        var result = telegram.Value.LogChatIds;
        return result.Length == 0 ? telegram.Value.AdminIds : result;
    }
}
