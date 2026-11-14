using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MARS.SoundRequest.Controllers;

/// <summary>
/// Внутренние межсервисные эндпоинты MARS.SoundRequest.
/// </summary>
/// <remarks>
/// Доступны только с корректным <c>X-Api-Key</c> (политика <c>ServiceApiKey</c>),
/// в отличие от публичного <see cref="SoundRequestController"/>, который нужен
/// пульту плеера. Глушит и возвращает звук координатор в MARS.Alerts: плеер для
/// него — одна из ног, а не владелец.
/// </remarks>
[ApiController]
[Route("api/internal/sound-request")]
[Authorize(Policy = ServiceAuthExtensions.PolicyName)]
public class InternalSoundRequestController(
    StateManager stateManager,
    ILogger<InternalSoundRequestController> logger
) : ControllerBase
{
    /// <summary>
    /// Глушит плеер: ставит флаг IsMuted и приостанавливает воспроизведение.
    /// </summary>
    [HttpPost("mute")]
    public async Task<ActionResult<OperationResult>> MuteAll(
        CancellationToken cancellationToken = default
    )
    {
        var state = stateManager.GetState();

        if (state.State == PlaybackState.Playing)
        {
            await stateManager.SetPausedAsync(true);
            await stateManager.SetPausedByMuteAsync(true);
        }

        await stateManager.SetMutedAsync(true);

        logger.LogInformation("Плеер заглушён координатором глушения звука");

        return Ok(OperationResult.Ok());
    }

    /// <summary>
    /// Снимает mute с плеера: снимает флаг IsMuted и возобновляет воспроизведение,
    /// если оно было приостановлено именно этим mute.
    /// </summary>
    [HttpPost("unmute")]
    public async Task<ActionResult<OperationResult>> UnmuteAll(
        CancellationToken cancellationToken = default
    )
    {
        await stateManager.SetMutedAsync(false);

        var state = stateManager.GetState();

        if (state.PausedByMute)
        {
            await stateManager.SetPausedAsync(false);
            await stateManager.SetPausedByMuteAsync(false);
        }

        logger.LogInformation("Mute снят с плеера координатором глушения звука");

        return Ok(OperationResult.Ok());
    }
}
