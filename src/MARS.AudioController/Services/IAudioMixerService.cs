namespace MARS.AudioController.Services;

/// <summary>
/// Доступ к микшеру приложений (аудиосессиям процессов). Абстракция над NAudio
/// AudioSessionManager, чтобы контроллеры были тестируемыми без реального микшера.
/// </summary>
public interface IAudioMixerService
{
    /// <summary>
    /// Глушит все аудиосессии процессов, кроме тех, чьи имена содержат хотя бы одно
    /// из строк <paramref name="excludedProcessNames"/>. Возвращает список имён
    /// заглушенных процессов.
    /// </summary>
    Task<IReadOnlyList<string>> MuteAsync(
        IReadOnlyList<string> excludedProcessNames,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Снимает mute со всех сессий, которые были заглушены этим сервисом.
    /// Возвращает количество разблокированных сессий.
    /// </summary>
    Task<int> UnmuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Список сессией, которые сейчас заглушены этим сервисом.
    /// </summary>
    Task<IReadOnlyList<MutedSessionInfo>> GetMutedSessionsAsync(
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Информация об одной заглушённой аудиосессии.
/// </summary>
public sealed record MutedSessionInfo(uint ProcessId, string ProcessName, string DisplayName);
