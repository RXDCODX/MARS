using MARS.AudioController.Configuration;
using MARS.AudioController.Services;
using MARS.Shared.Models;
using MARS.Shared.Models.Audio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MARS.AudioController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MixerController(
    IAudioMixerService audioMixerService,
    IOptions<AudioControllerConfiguration> configuration,
    ILogger<MixerController> logger
) : ControllerBase
{
    [HttpPost("mute")]
    public async Task<ActionResult<OperationResult<MuteMixerResponse>>> Mute(
        [FromBody] MuteMixerRequest? request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var excludedProcessNames =
                request?.ExcludedProcessNames ?? configuration.Value.DefaultExcludedProcessNames;

            var mutedProcessNames = await audioMixerService.MuteAsync(
                excludedProcessNames,
                cancellationToken
            );

            var response = new MuteMixerResponse
            {
                MutedSessionCount = mutedProcessNames.Count,
                MutedProcessNames = mutedProcessNames,
            };

            return Ok(OperationResult<MuteMixerResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute audio sessions");
            return Ok(
                OperationResult<MuteMixerResponse>.Fail(
                    $"Failed to mute audio sessions: {ex.Message}"
                )
            );
        }
    }

    [HttpPost("unmute")]
    public async Task<ActionResult<OperationResult<UnmuteMixerResponse>>> Unmute(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var count = await audioMixerService.UnmuteAsync(cancellationToken);

            var response = new UnmuteMixerResponse { UnmutedSessionCount = count };

            return Ok(OperationResult<UnmuteMixerResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute audio sessions");
            return Ok(
                OperationResult<UnmuteMixerResponse>.Fail(
                    $"Failed to unmute audio sessions: {ex.Message}"
                )
            );
        }
    }

    [HttpGet("muted")]
    public async Task<ActionResult<OperationResult<MutedSessionsResponse>>> GetMutedSessions(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var sessions = await audioMixerService.GetMutedSessionsAsync(cancellationToken);

            var response = new MutedSessionsResponse
            {
                Sessions = sessions
                    .Select(session => new MutedSessionDto
                    {
                        ProcessId = session.ProcessId,
                        ProcessName = session.ProcessName,
                        DisplayName = session.DisplayName,
                    })
                    .ToList(),
            };

            return Ok(OperationResult<MutedSessionsResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get muted sessions");
            return Ok(
                OperationResult<MutedSessionsResponse>.Fail(
                    $"Failed to get muted sessions: {ex.Message}"
                )
            );
        }
    }
}
