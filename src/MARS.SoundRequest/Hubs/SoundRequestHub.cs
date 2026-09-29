using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using Microsoft.AspNetCore.SignalR;

namespace MARS.SoundRequest.Hubs;

public class SoundRequestHub(
    OutSignalRHubService service,
    StateManager stateManager,
    MainPlayer mainPlayer
) : Hub<ISoundRequestHub>
{
    public override Task OnConnectedAsync()
    {
        return Clients.Caller.PlayerStateChange(stateManager.GetState());
    }

    /// <summary>
    /// Вызывается фронтендом когда трек завершил воспроизведение
    /// </summary>
    public Task Ended(BaseTrackInfo info)
    {
        return service.OnEndedInvoke(info);
    }

    /// <summary>
    /// Вызывается фронтендом когда трек начал воспроизведение
    /// </summary>
    public Task Started(BaseTrackInfo info)
    {
        return service.OnStartedInvoke(info);
    }

    /// <summary>
    /// Вызывается фронтендом при ошибке воспроизведения
    /// </summary>
    public Task ErrorPlaying(BaseTrackInfo info)
    {
        return service.OnErrorInvoke(info);
    }

    /// <summary>
    /// Вызывается фронтендом при изменении состояния плеера
    /// </summary>
    public async Task FrontStateChange(PlayerState newState)
    {
        if (newState.State == PlaybackState.Playing)
        {
            await mainPlayer.EnsureCurrentQueueItemLoadedAsync();
        }

        await stateManager.UpdateStateAsync(state =>
        {
            state.State = newState.State;
            state.IsMuted = newState.IsMuted;
            state.Volume = newState.Volume;
            state.VideoState = newState.VideoState;
            state.CurrentTrackProgress = newState.CurrentTrackProgress;
        });
    }

    /// <summary>
    /// Вызывается фронтендом для обновления прогресса воспроизведения трека
    /// </summary>
    public Task TrackProgress(long seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return stateManager.UpdateCurrentTrackProgressAsync(
            span,
            notify: false,
            excludeConnectionId: Context.ConnectionId
        );
    }

    /// <summary>
    /// Вызывается фронтендом для переключения на следующий трек
    /// </summary>
    public Task SkipTrack()
    {
        return mainPlayer.SkipAsync(CancellationToken.None);
    }

    /// <summary>
    /// Вызывается фронтендом для переключения на предыдущий трек из истории
    /// </summary>
    public Task PlayPrevious()
    {
        return mainPlayer.PlayPreviousFromHistoryAsync();
    }
}
