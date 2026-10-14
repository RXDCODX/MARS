using System;
using System.Threading.Tasks;
using MARS.Shared.Clients;
using MARS.Shared.Grpc.SoundRequest;
using MARS.Shared.Grpc.Voice;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Audio;

/// <summary>
/// Координатор одновременного глушения звука на всех подписанных сервисах:
/// микшер приложений (AudioController), TTS и плеер SoundRequest.
/// </summary>
/// <remarks>
/// Каждая нога — в своём <c>try</c>: ошибка одной не должна ронять остальные.
/// </remarks>
public class SoundMuteCoordinator(
    IAudioControllerClient audioControllerClient,
    VoiceRecognitionService.VoiceRecognitionServiceClient ttsGrpcClient,
    SoundRequestService.SoundRequestServiceClient soundRequestGrpcClient,
    ILogger<SoundMuteCoordinator> logger
)
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
            await audioControllerClient.MuteAsync(excludedProcessNames);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute audio sessions on AudioController");
        }

        try
        {
            await soundRequestGrpcClient.MuteAllAsync(new MuteAllRequest());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute SoundRequest player");
        }

        try
        {
            if (!_isMuted)
            {
                _previousTtsVolume = 1.0;
                await ttsGrpcClient.SetMuteStateAsync(
                    new SetMuteStateRequest { IsMuted = true, Volume = 0.0 }
                );
                _isMuted = true;
            }
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
            await audioControllerClient.UnmuteAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute audio sessions on AudioController");
        }

        try
        {
            await soundRequestGrpcClient.UnmuteAllAsync(new UnmuteAllRequest());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute SoundRequest player");
        }

        try
        {
            var volumeToRestore = _previousTtsVolume ?? 1.0;
            await ttsGrpcClient.SetMuteStateAsync(
                new SetMuteStateRequest { IsMuted = false, Volume = volumeToRestore }
            );
            _isMuted = false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute TTS");
        }
    }
}
