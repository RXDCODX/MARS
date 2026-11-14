using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.Shared.Contracts.Models.Control;
using MARS.TTS.Models;
using MARS.TTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.TTS.Controllers;

/// <summary>
/// Внутренние межсервисные эндпоинты MARS.TTS.
/// </summary>
/// <remarks>
/// Доступны только с корректным <c>X-Api-Key</c> (политика <c>ServiceApiKey</c>).
/// Глушением TTS управляет координатор в MARS.Alerts: он глушит микшер, плеер и
/// синтез одновременно, и синтез для него — одна из ног, а не владелец.
/// </remarks>
[ApiController]
[Route("api/internal/tts")]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
public class TtsInternalController(ITtsNotifier notifier, ILogger<TtsInternalController> logger)
    : ControllerBase
{
    /// <summary>
    /// Выставляет состояние глушения и рассылает его подписчикам TTS.
    /// </summary>
    [HttpPost("mute-state")]
    public async Task<ActionResult<OperationResult>> SetMuteState(
        [FromBody] TtsMuteStateRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is null)
        {
            return BadRequest(OperationResult.Fail("Состояние не передано"));
        }

        logger.LogInformation(
            "SetMuteState: IsMuted={IsMuted}, Volume={Volume}",
            request.IsMuted,
            request.Volume
        );

        await notifier.BroadcastStateAsync(
            new TtsState { IsStopped = request.IsMuted, Volume = request.Volume },
            cancellationToken
        );

        return Ok(OperationResult.Ok());
    }
}
