using MARS.Shared.Models;
using MARS.Telegram.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController(
    IDbContextFactory<ChatDbContext> dbFactory,
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
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var count = await db.TelegramUsers.AsNoTracking().CountAsync(cancellationToken);
            return Ok(OperationResult<int>.Ok(count));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user count");
            return Ok(OperationResult<int>.Fail("Ошибка при получении количества пользователей"));
        }
    }
}
