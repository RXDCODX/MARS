using MARS.Admin.Entities;
using MARS.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using MARS.Admin.Services;
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
    public ActionResult<OperationResult<RateLimiterInfo?>> GetRateLimiterInfo()
    {
        ActionResult<OperationResult<RateLimiterInfo?>> result;
        try
        {
            var info = shikimoriService.GetRateLimiterInfo();
            result = Ok(
                OperationResult<RateLimiterInfo?>.Ok("Информация о рейт лимитере получена", info)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении информации о рейт лимитере");
            result = Ok(
                OperationResult<RateLimiterInfo?>.Bad(
                    "Ошибка при получении информации о рейт лимитере",
                    null
                )
            );
        }

        return result;
    }
}
