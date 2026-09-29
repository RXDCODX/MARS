using MARS.Discord.Models;
using MARS.Discord.Services.Gateway;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Discord.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscordController(
    IDiscordGatewayService gatewayService,
    ILogger<DiscordController> logger
) : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<OperationResult<object>> GetStatus()
    {
        var isConnected = gatewayService.IsConnected;
        return Ok(OperationResult<object>.Ok("Status retrieved", new { IsConnected = isConnected }));
    }

    [HttpPost("send")]
    public async Task<ActionResult<OperationResult>> SendMessage(
        [FromQuery] ulong channelId,
        [FromQuery] string message,
        CancellationToken cancellationToken = default
    )
    {
        var result = await gatewayService.SendMessageAsync(channelId, message, cancellationToken);

        if (!result)
        {
            logger.LogWarning(
                "SendMessage failed for channel {ChannelId}: {Message}",
                channelId,
                result.Message
            );
        }

        return Ok(result);
    }
}
