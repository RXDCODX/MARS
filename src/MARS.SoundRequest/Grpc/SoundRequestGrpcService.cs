using Grpc.Core;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.Interfaces;

namespace MARS.SoundRequest.Grpc;

public sealed class SoundRequestGrpcService(
    StateManager stateManager,
    IPlayerController playerController,
    TrackEventRelay trackEventRelay,
    GrpcEventBroadcaster<SoundRequestEvent> broadcaster
) : SoundRequestService.SoundRequestServiceBase
{
    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<SoundRequestEvent> responseStream,
        ServerCallContext context
    )
    {
        using var subscription = broadcaster.Subscribe();

        broadcaster.TryEnqueue(
            subscription,
            new SoundRequestEvent
            {
                PlayerStateChange = SoundRequestGrpcMapper.ToProto(stateManager.GetState()),
            }
        );

        await broadcaster.PumpAsync(subscription, responseStream, context.CancellationToken);
    }

    /// <summary>
    /// Вызывается фронтендом когда трек начал воспроизведение
    /// </summary>
    public override async Task<TrackEventResponse> Started(
        TrackEventRequest request,
        ServerCallContext context
    )
    {
        await trackEventRelay.OnStartedInvoke(RequireTrack(request.Track));

        return new TrackEventResponse();
    }

    /// <summary>
    /// Вызывается фронтендом когда трек завершил воспроизведение
    /// </summary>
    public override async Task<TrackEventResponse> Ended(
        TrackEventRequest request,
        ServerCallContext context
    )
    {
        await trackEventRelay.OnEndedInvoke(RequireTrack(request.Track));

        return new TrackEventResponse();
    }

    /// <summary>
    /// Вызывается фронтендом при ошибке воспроизведения
    /// </summary>
    public override async Task<TrackEventResponse> ErrorPlaying(
        TrackEventRequest request,
        ServerCallContext context
    )
    {
        await trackEventRelay.OnErrorInvoke(RequireTrack(request.Track));

        return new TrackEventResponse();
    }

    /// <summary>
    /// Вызывается фронтендом при изменении состояния плеера
    /// </summary>
    public override async Task<FrontStateChangeResponse> FrontStateChange(
        FrontStateChangeRequest request,
        ServerCallContext context
    )
    {
        var newState = SoundRequestGrpcMapper.ToDto(request.State);

        if (newState.State == PlaybackState.Playing)
        {
            await playerController.EnsureCurrentQueueItemLoadedAsync();
        }

        await stateManager.UpdateStateAsync(state =>
        {
            state.State = newState.State;
            state.IsMuted = newState.IsMuted;
            state.Volume = newState.Volume;
            state.VideoState = newState.VideoState;
            state.CurrentTrackProgress = newState.CurrentTrackProgress;
        });

        return new FrontStateChangeResponse();
    }

    /// <summary>
    /// Вызывается фронтендом для обновления прогресса воспроизведения трека
    /// </summary>
    public override async Task<TrackProgressResponse> TrackProgress(
        TrackProgressRequest request,
        ServerCallContext context
    )
    {
        var span = TimeSpan.FromSeconds(request.Seconds);

        await stateManager.UpdateCurrentTrackProgressAsync(
            span,
            notify: false,
            excludeSubscriberId: request.SubscriberId
        );

        return new TrackProgressResponse();
    }

    /// <summary>
    /// Вызывается фронтендом для переключения на следующий трек
    /// </summary>
    public override async Task<SkipTrackResponse> SkipTrack(
        SkipTrackRequest request,
        ServerCallContext context
    )
    {
        await playerController.SkipAsync(context.CancellationToken);

        return new SkipTrackResponse();
    }

    /// <summary>
    /// Вызывается фронтендом для переключения на предыдущий трек из истории
    /// </summary>
    public override async Task<PlayPreviousResponse> PlayPrevious(
        PlayPreviousRequest request,
        ServerCallContext context
    )
    {
        await playerController.PlayPreviousFromHistoryAsync();

        return new PlayPreviousResponse();
    }

    private static BaseTrackInfo RequireTrack(TrackInfo? track)
    {
        var result = track is null ? null : SoundRequestGrpcMapper.ToDto(track);

        if (result is null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Трек не передан"));
        }

        return result;
    }
}
