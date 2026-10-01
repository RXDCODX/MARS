using MARS.SoundRequest.Entities;

namespace MARS.SoundRequest.Services.Interfaces;

public interface IPlayerController
{
    Task PauseAsync(CancellationToken ct);
    Task ResumeAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SkipAsync(CancellationToken ct);
    Task SetVolumeAsync(float volume, CancellationToken ct);
    Task MuteAsync(CancellationToken ct);
    Task UnmuteAsync(CancellationToken ct);
    Task SetVideoDisplayAsync(VideoDisplay videoDisplay, CancellationToken ct);

    /// <summary>
    /// Подгружает текущий элемент очереди в состояние плеера, если он пуст.
    /// </summary>
    Task EnsureCurrentQueueItemLoadedAsync();

    /// <summary>
    /// Возвращает воспроизведение на предыдущий трек из истории.
    /// </summary>
    Task PlayPreviousFromHistoryAsync();

    PlayerState GetState();
}
