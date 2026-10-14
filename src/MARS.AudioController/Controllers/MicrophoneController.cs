using MARS.AudioController.Services;
using MARS.Shared.Models;
using MARS.Shared.Models.Audio;
using Microsoft.AspNetCore.Mvc;

namespace MARS.AudioController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MicrophoneController(
    IAudioDeviceService audioDeviceService,
    ILogger<MicrophoneController> logger
) : ControllerBase
{
    [HttpGet("volume")]
    public async Task<ActionResult<OperationResult<MicrophoneVolumeResponse>>> GetVolume(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var volume = await audioDeviceService.GetVolumeAsync(cancellationToken);
            var deviceName = await audioDeviceService.GetDeviceNameAsync(cancellationToken);
            var isMuted = await audioDeviceService.IsMutedAsync(cancellationToken);

            var response = new MicrophoneVolumeResponse
            {
                Volume = volume,
                VolumePercent = $"{volume * 100:F0}%",
                IsMuted = isMuted,
                IsAtMaximum = volume >= 1.0f,
                DeviceName = deviceName,
            };

            return Ok(OperationResult<MicrophoneVolumeResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get microphone volume");
            return Ok(
                OperationResult<MicrophoneVolumeResponse>.Fail(
                    $"Failed to get microphone volume: {ex.Message}"
                )
            );
        }
    }

    [HttpPost("volume/{volume:float}")]
    public async Task<ActionResult<OperationResult<MicrophoneVolumeSetResponse>>> SetVolume(
        float volume,
        CancellationToken cancellationToken = default
    )
    {
        if (volume < 0.0f || volume > 1.0f)
        {
            return Ok(
                OperationResult<MicrophoneVolumeSetResponse>.Fail(
                    "Volume must be between 0.0 and 1.0"
                )
            );
        }

        try
        {
            var previousVolume = await audioDeviceService.GetVolumeAsync(cancellationToken);

            await audioDeviceService.SetVolumeAsync(volume, cancellationToken);

            var response = new MicrophoneVolumeSetResponse
            {
                PreviousVolume = previousVolume,
                PreviousVolumePercent = $"{previousVolume * 100:F0}%",
                CurrentVolume = volume,
                CurrentVolumePercent = $"{volume * 100:F0}%",
                Message = $"Microphone volume set to {volume * 100:F0}%",
            };

            return Ok(OperationResult<MicrophoneVolumeSetResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set microphone volume to {Volume}", volume);
            return Ok(
                OperationResult<MicrophoneVolumeSetResponse>.Fail(
                    $"Failed to set microphone volume: {ex.Message}"
                )
            );
        }
    }

    [HttpPost("volume/max")]
    public async Task<ActionResult<OperationResult<MicrophoneVolumeSetResponse>>> SetVolumeToMax(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var previousVolume = await audioDeviceService.GetVolumeAsync(cancellationToken);

            await audioDeviceService.SetVolumeToMaxAsync(cancellationToken);

            var response = new MicrophoneVolumeSetResponse
            {
                PreviousVolume = previousVolume,
                PreviousVolumePercent = $"{previousVolume * 100:F0}%",
                CurrentVolume = 1.0f,
                CurrentVolumePercent = "100%",
                Message = "Microphone volume set to maximum",
            };

            return Ok(OperationResult<MicrophoneVolumeSetResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set microphone volume to maximum");
            return Ok(
                OperationResult<MicrophoneVolumeSetResponse>.Fail(
                    $"Failed to set microphone volume to maximum: {ex.Message}"
                )
            );
        }
    }
}
