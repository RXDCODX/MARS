using System.Diagnostics;
using MARS.AudioController.Services;
using NAudio.CoreAudioApi;

namespace MARS.AudioController.Services.Naaudio;

/// <summary>
/// Реализация <see cref="IAudioMixerService"/> на базе NAudio Core Audio API.
/// Глушит аудиосессии процессов по подстроке в имени процесса, кроме указанных
/// исключений. Работает только на Windows.
/// </summary>
public sealed class NaaudioAudioMixerService : IAudioMixerService
{
    private readonly List<AudioSessionControl> _mutedSessions = new();
    private readonly SemaphoreSlim _semaphore = new(1);

    public async Task<IReadOnlyList<string>> MuteAsync(
        IReadOnlyList<string> excludedProcessNames,
        CancellationToken cancellationToken = default
    )
    {
        await _semaphore.WaitAsync(cancellationToken);

        var mutedProcessNames = new List<string>();

        try
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessions = device.AudioSessionManager.Sessions;

            for (var index = 0; index < sessions.Count; index++)
            {
                var session = sessions[index];

                try
                {
                    var processName = GetProcessName(session);
                    var isExcluded = excludedProcessNames.Any(excluded =>
                        processName.Contains(excluded, StringComparison.OrdinalIgnoreCase)
                    );

                    if (!isExcluded && !session.SimpleAudioVolume.Mute)
                    {
                        session.SimpleAudioVolume.Mute = true;
                        _mutedSessions.Add(session);
                        mutedProcessNames.Add(processName);
                    }
                }
                catch
                {
                    // Сессия могла завершиться между перечислением и обращением.
                }
            }

            return mutedProcessNames;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> UnmuteAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var count = _mutedSessions.Count;

            foreach (var session in _mutedSessions.ToList())
            {
                try
                {
                    session.SimpleAudioVolume.Mute = false;
                }
                catch
                {
                    // Сессия могла завершиться.
                }
            }

            _mutedSessions.Clear();

            return count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<MutedSessionInfo>> GetMutedSessionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return _mutedSessions
                .Select(session => new MutedSessionInfo(
                    session.GetProcessID,
                    GetProcessName(session),
                    session.DisplayName
                ))
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string GetProcessName(AudioSessionControl session)
    {
        try
        {
            return session.GetProcessID == 0
                ? "System"
                : Process.GetProcessById((int)session.GetProcessID).ProcessName;
        }
        catch
        {
            return "Unknown";
        }
    }
}
