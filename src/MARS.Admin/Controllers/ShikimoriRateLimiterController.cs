using MARS.Admin.Services;
using MARS.Shared.Clients;
using MARS.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Admin.Controllers;

/// <summary>
/// Контроллер для мониторинга состояния рейт лимитера Shikimori API
/// </summary>
[ApiController]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
[Route("api/[controller]")]
public class ShikimoriRateLimiterController(
    IShikimoriRateLimiterService shikimoriService,
    ILogger<ShikimoriRateLimiterController> logger
) : ControllerBase
{
    /// <summary>
    /// Получает информацию о текущем состоянии рейт лимитера
    /// </summary>
    [HttpGet("info")]
    public async Task<ActionResult<OperationResult<ShikimoriRateLimiterInfo?>>> GetRateLimiterInfo(
        CancellationToken cancellationToken = default
    )
    {
        ActionResult<OperationResult<ShikimoriRateLimiterInfo?>> result;

        try
        {
            var info = await shikimoriService.GetRateLimiterInfoAsync(cancellationToken);

            result = info is null
                ? Ok(
                    OperationResult<ShikimoriRateLimiterInfo?>.Bad(
                        "MARS.Shikimori недоступен",
                        null
                    )
                )
                : Ok(
                    OperationResult<ShikimoriRateLimiterInfo?>.Ok(
                        "Информация о рейт лимитере получена",
                        info
                    )
                );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении информации о рейт лимитере");
            result = Ok(
                OperationResult<ShikimoriRateLimiterInfo?>.Bad(
                    "Ошибка при получении информации о рейт лимитере",
                    null
                )
            );
        }

        return result;
    }
}
