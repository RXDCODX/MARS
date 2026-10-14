using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Shared.Models.Audio;

namespace MARS.Shared.Clients;

/// <summary>
/// HTTP-клиент MARS.AudioController — сервиса управления громкостью микрофона
/// и микшером приложений. Работает на Windows-хосте, вне Docker.
/// </summary>
public interface IAudioControllerClient
{
    /// <summary>
    /// Глушит аудиосессии процессов, кроме указанных исключений.
    /// </summary>
    Task<MuteMixerResponse?> MuteAsync(
        IReadOnlyList<string>? excludedProcessNames = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Снимает mute со всех сессий, заглушенных этим клиентом.
    /// </summary>
    Task<UnmuteMixerResponse?> UnmuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Список сессий, которые сейчас заглушены.
    /// </summary>
    Task<MutedSessionsResponse?> GetMutedSessionsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Текущая громкость микрофона.
    /// </summary>
    Task<MicrophoneVolumeResponse?> GetMicrophoneVolumeAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Устанавливает громкость микрофона.
    /// </summary>
    Task<MicrophoneVolumeSetResponse?> SetMicrophoneVolumeAsync(
        float volume,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Устанавливает громкость микрофона на максимум.
    /// </summary>
    Task<MicrophoneVolumeSetResponse?> SetMicrophoneVolumeToMaxAsync(
        CancellationToken cancellationToken = default
    );
}
