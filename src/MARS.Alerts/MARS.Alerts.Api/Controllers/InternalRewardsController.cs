using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Alerts.Api.Controllers;

/// <summary>
/// Внутренний эндпоинт для получения определений наград и их планировочной
/// активности из MARS.Alerts (вызывается MARS.Twitch для синхронизации).
/// Не маршрутизируется через Gateway: service-to-service по ServiceApiKey.
/// </summary>
[ApiController]
[Route("api/internal/rewards")]
[Authorize(Policy = "ServiceApiKey")]
public class InternalRewardsController(
    IEnumerable<IRewardAlertHandler> handlers,
    ILogger<InternalRewardsController> logger
) : ControllerBase
{
    [HttpGet("definitions")]
    public async Task<ActionResult<OperationResult<List<RewardDefinitionSnapshot>>>> GetDefinitions(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var list = new List<RewardDefinitionSnapshot>();
            foreach (var handler in handlers)
            {
                var isActive = await handler.Activation.IsActiveForSchedulingAsync(
                    cancellationToken
                );
                list.Add(
                    new RewardDefinitionSnapshot(
                        handler.RewardCost,
                        handler.AlertDisplayName,
                        handler.AlertDescription,
                        $"#{handler.Color.R:X2}{handler.Color.G:X2}{handler.Color.B:X2}",
                        handler.IsUserInputRequired,
                        isActive
                    )
                );
            }

            return Ok(OperationResult<List<RewardDefinitionSnapshot>>.Ok(list));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения определений наград");
            return Ok(
                OperationResult<List<RewardDefinitionSnapshot>>.Fail($"Ошибка: {ex.Message}")
            );
        }
    }
}
