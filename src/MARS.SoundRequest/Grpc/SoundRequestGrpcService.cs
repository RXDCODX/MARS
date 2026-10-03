using Grpc.Core;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Hubs.Dtos;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.Interfaces;

namespace MARS.SoundRequest.Grpc;

public sealed class SoundRequestGrpcService(
    StateManager stateManager,
    GrpcEventBroadcaster<SoundRequestEvent> broadcaster,
    ISoundRequestPlayback playback
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
    /// <remarks>
    /// Тела команд лежат в <c>ISoundRequestPlayback</c>: те же семь команд
    /// нужны и хабу, потому что браузер до gRPC не ходит. Пока логика жила
    /// здесь, пульт плеера и видеоэкран не делали ничего.
    /// </remarks>
    public override async Task<TrackEventResponse> Started(
        TrackEventRequest request,
        ServerCallContext context
    )
    {
        await playback.StartedAsync(RequireTrack(request.Track), context.CancellationToken);

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
        await playback.EndedAsync(RequireTrack(request.Track), context.CancellationToken);

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
        await playback.ErrorPlayingAsync(RequireTrack(request.Track), context.CancellationToken);

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
        await playback.FrontStateChangeAsync(
            SoundRequestHubMapper.ToHubState(request.State),
            context.CancellationToken
        );

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
        await playback.TrackProgressAsync(
            request.Seconds,
            request.SubscriberId,
            context.CancellationToken
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
        await playback.SkipAsync(context.CancellationToken);

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
        await playback.PlayPreviousAsync(context.CancellationToken);

        return new PlayPreviousResponse();
    }

    /// <summary>
    /// Трек обязателен: без него событие старта или окончания некуда адресовать.
    /// </summary>
    private static TrackInfoHubDto RequireTrack(TrackInfo? track)
    {
        var result = track is null ? null : SoundRequestHubMapper.ToHubTrack(track);

        if (result is null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Трек не передан"));
        }

        return result;
    }
}
