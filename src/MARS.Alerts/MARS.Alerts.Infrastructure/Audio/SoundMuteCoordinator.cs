using System;
using System.Threading.Tasks;
using Grpc.Core;
using MARS.Alerts.Infrastructure.Overlay;
using MARS.Platform.Infrastructure.Grpc;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Infrastructure.Audio;

/// <summary>
/// Координатор одновременного глушения звука на всех подписанных сервисах:
/// микшер приложений (AudioController), TTS и плеер SoundRequest.
/// </summary>
/// <remarks>
/// Каждая нога — в своём <c>try</c>: ошибка одной не должна ронять остальные.
/// </remarks>
public class SoundMuteCoordinator(IMarsGrpcClient grpcClient, ILogger<SoundMuteCoordinator> logger)
    : ISoundMuteCoordinator
{
    private double? _previousTtsVolume;
    private bool _isMuted;

    /// <summary>
    /// Глушит все три ноги одновременно.
    /// </summary>
    /// <param name="excludedProcessNames">
    /// Список имён процессов-исключений для микшера. <c>null</c> или пустой массив
    /// означают значения по умолчанию из конфигурации AudioController.
    /// </param>
    public async Task MuteAsync(string[]? excludedProcessNames = null)
    {
        try
        {
            await grpcClient.AudioController.MuteAsync(excludedProcessNames);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute audio sessions on AudioController");
        }

        try
        {
            await grpcClient.SoundRequest.MuteAllAsync();
        }
        catch (RpcException)
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
        catch (RpcException)
        {
            logger.LogWarning("TTS was not muted: the service is unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute TTS");
        }
    }

    /// <summary>
    /// Снимает mute со всех ног.
    /// </summary>
    public async Task UnmuteAsync()
    {
        try
        {
            await grpcClient.AudioController.UnmuteAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute audio sessions on AudioController");
        }

        try
        {
            await grpcClient.SoundRequest.UnmuteAllAsync();
        }
        catch (RpcException)
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
        catch (RpcException)
        {
            logger.LogWarning("TTS was not unmuted: the service is unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute TTS");
        }
    }
}
