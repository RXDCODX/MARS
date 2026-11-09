using MARS.Shared.Contracts.Models;
using MARS.Telegram.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Telegram.Api.Rest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController(
    ITelegramUserQueryService userQueries,
    ILogger<TelegramController> logger
) : ControllerBase
{
    [HttpGet("health")]
    public ActionResult<OperationResult<string>> Health()
    {
        return Ok(OperationResult<string>.Ok("MARS.Telegram is healthy"));
    }

    [HttpGet("users")]
    public async Task<ActionResult<OperationResult<int>>> GetUserCount(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var count = await userQueries.GetUserCountAsync(cancellationToken);
            return Ok(OperationResult<int>.Ok(count));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user count");
            return Ok(OperationResult<int>.Fail("Ошибка при получении количества пользователей"));
        }
    }
}
