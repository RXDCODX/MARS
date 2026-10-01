using MARS.SoundRequest.Entities;

namespace MARS.SoundRequest.Services;

/// <summary>
/// Релей событий воспроизведения, которые сообщает плеер на странице.
/// Сигналы от gRPC-вызовов (Started/Ended/ErrorPlaying) идут в MainPlayer через него.
/// </summary>
public class TrackEventRelay
{
    #region IPlayerController Events

    /// <summary>
    /// Событие начала воспроизведения трека
    /// </summary>
    public event Func<BaseTrackInfo, Task>? OnStarted;

    /// <summary>
    /// Событие завершения воспроизведения трека
    /// </summary>
    public event Func<BaseTrackInfo, Task>? OnEnded;

    /// <summary>
    /// Событие ошибки воспроизведения
    /// </summary>
    public event Func<BaseTrackInfo, Task>? OnError;

    #endregion

    public Task OnStartedInvoke(BaseTrackInfo info)
    {
        return OnStarted?.Invoke(info) ?? Task.CompletedTask;
    }

    public Task OnEndedInvoke(BaseTrackInfo info)
    {
        return OnEnded?.Invoke(info) ?? Task.CompletedTask;
    }

    public Task OnErrorInvoke(BaseTrackInfo info)
    {
        return OnError?.Invoke(info) ?? Task.CompletedTask;
    }
}
