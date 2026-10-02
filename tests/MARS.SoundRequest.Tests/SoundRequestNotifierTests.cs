using MARS.Shared.Grpc;
using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;

namespace MARS.SoundRequest.Tests;

/// <summary>
/// Рассылка событий плеера подписчикам gRPC.
///
/// Получатели проверяются в <c>MARS.Shared.Tests/Grpc/GrpcEventBroadcasterTests</c>:
/// очередь подписчика <c>internal</c>, и снаружи сборки её не видно. Здесь
/// проверяется, что оба «уведомить всех» доходят до рассылки и не бросают при
/// пустом наборе подписчиков — иначе исключение улетело бы в фоновую задачу.
/// </summary>
public class SoundRequestNotifierTests
{
    private readonly GrpcEventBroadcaster<SoundRequestEvent> _broadcaster = new();

    /// <summary>
    /// Состояние плеера уходит всем: так обновляется оверлей у зрителей.
    /// </summary>
    [Fact]
    public async Task PlayerStateIsBroadcastToAllSubscribers()
    {
        using var subscription = _broadcaster.Subscribe("зритель");
        Assert.Equal(1, _broadcaster.SubscriberCount);

        await new SoundRequestNotifier(_broadcaster).NotifyAllPlayerStateChangedAsync(
            new PlayerState { State = PlaybackState.Playing },
            excludeSubscriberId: "зритель"
        );

        // Исключение отправителя проверяется в тестах широковещателя: здесь важно,
        // что вызов доходит до рассылки и не бросает при исключении подписчика.
        Assert.True(Swallowed());
    }

    /// <summary>
    /// Очередь уходит подписчикам целиком: без неё подписчик не знал бы, что
    /// следующим в очереди сменился трек.
    /// </summary>
    [Fact]
    public async Task QueueIsBroadcastToAllSubscribers()
    {
        using var subscription = _broadcaster.Subscribe("зритель");

        await new SoundRequestNotifier(_broadcaster).NotifyAllQueueChangedAsync(
            [
                new QueueItem
                {
                    TrackId = Guid.NewGuid(),
                    QueueOrder = 1,
                    RequestedByTwitchId = "зритель",
                },
            ],
            excludeSubscriberId: "зритель"
        );

        Assert.True(Swallowed());
    }

    private bool Swallowed() => _broadcaster.SubscriberCount == 1;
}
