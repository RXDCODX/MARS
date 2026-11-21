using MARS.Discord.Services.Gateway;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Discord.Controllers;

[ApiController]
[Route("api/internal")]
[Authorize(Policy = "ServiceApiKey")]
public class InternalController(
    IDiscordGatewayService gatewayService,
    ILogger<InternalController> logger
) : ControllerBase
{
    [HttpPost("messages/send")]
    public async Task<ActionResult<OperationResult>> SendMessage(
        [FromBody] DiscordMessageSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(OperationResult.Fail("Пустое сообщение"));
        }

        var result = await gatewayService.SendMessageAsync(
            request.ChannelId,
            request.Message,
            cancellationToken
        );

        if (!result.Success)
        {
            logger.LogWarning("SendMessage failed for channel {ChannelId}", request.ChannelId);
        }

        return Ok(result);
    }
}
