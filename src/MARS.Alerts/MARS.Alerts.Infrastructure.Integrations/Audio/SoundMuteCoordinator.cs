using System;
using System.Threading.Tasks;
using MARS.Alerts.Application.Overlay;
using MARS.Platform.Infrastructure.Clients.Http;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Infrastructure.Integrations.Audio;

/// <summary>
/// Координатор одновременного глушения звука на всех подписанных сервисах:
/// микшер приложений (AudioController), TTS и плеер SoundRequest.
/// </summary>
/// <remarks>
/// Каждая нога — в своём <c>try</c>: ошибка одной не должна ронять остальные.
/// </remarks>
public class SoundMuteCoordinator(
    IAudioControllerClient audioControllerClient,
    ITtsControlClient ttsControlClient,
    ISoundRequestControlClient soundRequestControlClient,
    ILogger<SoundMuteCoordinator> logger
) : ISoundMuteCoordinator
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
            // Клиент не бросает исключений: недоступность сервиса приходит
            // значением false, и это повод написать в лог, а не гасить ногу
            // молча.
            if (!await soundRequestControlClient.MuteAllAsync())
            {
                logger.LogWarning("SoundRequest player was not muted: the service is unavailable");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mute SoundRequest player");
        }

        try
        {
            if (!_isMuted)
            {
                var muted = await ttsControlClient.SetMuteStateAsync(isMuted: true, volume: 0.0);

                if (muted)
                {
                    _previousTtsVolume = 1.0;
                    _isMuted = true;
                }
                else
                {
                    // Флаг не выставляем: раз TTS не заглушён, снимать с него mute
                    // при следующем UnmuteAsync нечего, и громкость восстанавливать
                    // не из чего.
                    logger.LogWarning("TTS was not muted: the service is unavailable");
                }
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
            if (!await soundRequestControlClient.UnmuteAllAsync())
            {
                logger.LogWarning("SoundRequest player was not unmuted: the service is unavailable");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute SoundRequest player");
        }

        try
        {
            var volumeToRestore = _previousTtsVolume ?? 1.0;
            var unmuted = await ttsControlClient.SetMuteStateAsync(
                isMuted: false,
                volume: volumeToRestore
            );

            if (unmuted)
            {
                _isMuted = false;
            }
            else
            {
                logger.LogWarning("TTS was not unmuted: the service is unavailable");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unmute TTS");
        }
    }
}
