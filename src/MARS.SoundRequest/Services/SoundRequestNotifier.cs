using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Grpc;

namespace MARS.SoundRequest.Services;

/// <summary>
/// Рассылает события плеера подписчикам gRPC-сервиса SoundRequestService.
/// </summary>
public class SoundRequestNotifier(GrpcEventBroadcaster<SoundRequestEvent> broadcaster)
{
    /// <summary>
    /// Уведомить клиентов об изменении состояния плеера
    /// </summary>
    /// <param name="playerState">Текущее состояние плеера</param>
    /// <param name="excludeSubscriberId">
    /// ID подписчика, которого нужно исключить из рассылки (например, инициатор изменения)
    /// </param>
    public Task NotifyPlayerStateChangedAsync(
        PlayerState playerState,
        string? excludeSubscriberId = null
    )
    {
        return broadcaster.BroadcastExceptAsync(
            excludeSubscriberId,
            new SoundRequestEvent
            {
                PlayerStateChange = SoundRequestGrpcMapper.ToProto(playerState),
            }
        );
    }

    /// <summary>
    /// Уведомить клиентов об изменении очереди
    /// </summary>
    /// <param name="queue">Текущая очередь элементов</param>
    /// <param name="excludeSubscriberId">ID подписчика, которого нужно исключить из рассылки</param>
    public Task NotifyQueueChangedAsync(List<QueueItem> queue, string? excludeSubscriberId = null)
    {
        var queueState = new QueueState();
        queueState.Queue.AddRange(SoundRequestGrpcMapper.ToProto(queue));

        return broadcaster.BroadcastExceptAsync(
            excludeSubscriberId,
            new SoundRequestEvent { QueueChanged = queueState }
        );
    }

    /// <summary>
    /// Уведомить всех клиентов (включая не в группе "player")
    /// </summary>
    /// <param name="playerState">Текущее состояние плеера</param>
    /// <param name="excludeSubscriberId">ID подписчика, которого нужно исключить из рассылки</param>
    public Task NotifyAllPlayerStateChangedAsync(
        PlayerState playerState,
        string? excludeSubscriberId = null
    )
    {
        return NotifyPlayerStateChangedAsync(playerState, excludeSubscriberId);
    }

    /// <summary>
    /// Уведомить всех клиентов об изменении очереди
    /// </summary>
    /// <param name="queue">Текущая очередь элементов</param>
    /// <param name="excludeSubscriberId">ID подписчика, которого нужно исключить из рассылки</param>
    public Task NotifyAllQueueChangedAsync(
        List<QueueItem> queue,
        string? excludeSubscriberId = null
    )
    {
        return NotifyQueueChangedAsync(queue, excludeSubscriberId);
    }
}
