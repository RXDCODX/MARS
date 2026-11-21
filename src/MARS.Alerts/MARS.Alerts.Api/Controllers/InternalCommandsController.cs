using MARS.Alerts.Application.Twitch.Rewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Alerts.Api.Controllers;

[ApiController]
[Route("api/internal/commands")]
[Authorize(Policy = "ServiceApiKey")]
public class InternalCommandsController(
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<InternalCommandsController> logger
) : ControllerBase
{
    [HttpPost("invoke")]
    public async Task<ActionResult<OperationResult<string>>> Invoke(
        [FromBody] AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        if (payload is null)
        {
            return Ok(OperationResult<string>.Fail("Пустой запрос."));
        }

        var handler = handlers
            .OfType<RewardHandlerBase>()
            .FirstOrDefault(h => h.CommandKey == PayloadKey(payload));

        if (handler is null)
        {
            logger.LogWarning("Не найден обработчик для payload {Type}", payload.GetType().Name);
            return Ok(OperationResult<string>.Fail("Команда не поддерживается."));
        }

        try
        {
            var result = await handler.InvokeFromCommandAsync(payload, cancellationToken);
            return Ok(OperationResult<string>.Ok(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка вызова команды алерта");
            return Ok(OperationResult<string>.Fail($"Ошибка: {ex.Message}"));
        }
    }

    private static string? PayloadKey(AlertCommandPayload payload) =>
        payload switch
        {
            AdhdAlertCommandPayload => "adhd",
            MikubeamAlertCommandPayload => "mikubeam",
            _ => null,
        };
}
