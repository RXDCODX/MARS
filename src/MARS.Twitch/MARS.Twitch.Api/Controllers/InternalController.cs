using MARS.Shared.Contracts.Messaging;
using MARS.Twitch.Infrastructure.Extensions;
using MARS.Twitch.Infrastructure.Services.Rewards;
using MARS.Twitch.Infrastructure.Services.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Client.Interfaces;

namespace MARS.Twitch.Api.Controllers;

/// <summary>
/// Internal-эндпоинты для межсервисных вызовов (MARS.Commands).
/// Закрыты политикой ServiceApiKey.
/// </summary>
[ApiController]
[Route("api/internal")]
[Authorize(Policy = "ServiceApiKey")]
public class InternalController(ITwitchClient client, MiniGamesManager miniGamesManager)
    : ControllerBase
{
    private const int MaxMessageLength = 500;

    [HttpPost("chat/send")]
    public async Task<ActionResult> SendChatMessage(
        [FromBody] TwitchChatSendRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return Ok();
        }

        var channel = string.IsNullOrWhiteSpace(request.Channel)
            ? TwitchConstants.Channel
            : request.Channel.Trim();

        var normalized = request
            .Message.Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("  ", " ")
            .Trim();

        while (normalized.Length > MaxMessageLength)
        {
            var splitAt = normalized.LastIndexOf(' ', MaxMessageLength);
            if (splitAt <= 0)
            {
                splitAt = MaxMessageLength;
            }

            await client.SendMessageAsync(channel, normalized[..splitAt]);
            normalized = normalized[splitAt..].TrimStart(' ');
        }

        if (normalized.Length > 0)
        {
            await client.SendMessageAsync(channel, normalized);
        }

        return Ok();
    }

    [HttpPost("commands/minigamestop")]
    public async Task<ActionResult<OperationResult>> StopMiniGames(
        CancellationToken cancellationToken = default
    )
    {
        await miniGamesManager.CancelAllGamesAsync();
        return Ok();
    }
}
