using System;
using System.Threading.Tasks;
using MARS.Alerts.Infrastructure.Overlay;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Infrastructure.Audio;

/// <summary>
/// Координатор одновременного глушения звука на всех подписанных сервисах:
/// микшер приложений (AudioController), TTS и плеер SoundRequest.
/// </summary>
/// <remarks>
/// Каждая нога — в своём <c>try</c>: ошибка одной не должна ронять остальные.
/// AudioController недоступен — исключение пробрасывается после попытки остальных ног.
/// </remarks>
public class SoundMuteCoordinator(
    IAudioControllerClient audioControllerClient,
    IMarsGrpcClient grpcClient,
    ILogger<SoundMuteCoordinator> logger
) : ISoundMuteCoordinator
{
    private double? _previousTtsVolume;
    private bool _isMuted;

    public async Task MuteAsync(string[]? excludedProcessNames = null)
    {
        Exception? audioControllerError = null;

        try
        {
            await audioControllerClient.MuteAsync(excludedProcessNames);
        }
        catch (AudioControllerUnavailableException ex)
        {
            logger.LogError(ex, "Failed to mute audio sessions on AudioController");
            audioControllerError = ex;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute audio sessions on AudioController");
            audioControllerError = ex;
        }

        try
        {
            await grpcClient.SoundRequest.MuteAllAsync();
        }
        catch (Grpc.Core.RpcException)
        {
            logger.LogWarning("SoundRequest player was not muted: the service is unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute SoundRequest player");
        }

        try
        {
            if (!_isMuted)
            {
                await grpcClient.Tts.SetMuteStateAsync(isMuted: true, volume: 0.0);

                _previousTtsVolume = 1.0;
                _isMuted = true;
            }
        }
        catch (Grpc.Core.RpcException)
        {
            logger.LogWarning("TTS was not muted: the service is unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute TTS");
        }

        if (audioControllerError is not null)
        {
            throw audioControllerError;
        }
    }

    public async Task UnmuteAsync()
    {
        Exception? audioControllerError = null;

        try
        {
            await audioControllerClient.UnmuteAsync();
        }
        catch (AudioControllerUnavailableException ex)
        {
            logger.LogError(ex, "Failed to unmute audio sessions on AudioController");
            audioControllerError = ex;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute audio sessions on AudioController");
            audioControllerError = ex;
        }

        try
        {
            await grpcClient.SoundRequest.UnmuteAllAsync();
        }
        catch (Grpc.Core.RpcException)
        {
            logger.LogWarning("SoundRequest player was not unmuted: the service is unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute SoundRequest player");
        }

        try
        {
            var volumeToRestore = _previousTtsVolume ?? 1.0;
            await grpcClient.Tts.SetMuteStateAsync(isMuted: false, volume: volumeToRestore);

            _isMuted = false;
        }
        catch (Grpc.Core.RpcException)
        {
            logger.LogWarning("TTS was not unmuted: the service is unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute TTS");
        }

        if (audioControllerError is not null)
        {
            throw audioControllerError;
        }
    }
}
