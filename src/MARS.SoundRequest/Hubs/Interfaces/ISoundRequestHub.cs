using MARS.Shared.Grpc.SoundRequest;

namespace MARS.SoundRequest.Hubs.Interfaces;

/// <summary>
/// Методы хаба звуковых запросов, которые сервер шлёт подписчикам.
/// </summary>
/// <remarks>
/// Хаба на сервере не было, хотя <c>MARS.SoundRequest</c> уже держал
/// <c>GrpcEventBroadcaster&lt;SoundRequestEvent&gt;</c> и gRPC-сервис с той же
/// полезной нагрузкой. Клиент подписывался на <c>hubs/soundrequest</c> и
/// получал 405: путь не был объявлен в Gateway, запрос уходил в catch-all
/// клиента, а nginx отвечал на него отказом по методу.
/// <para>
/// Имена методов совпадают с ветками <c>oneof event</c> в
/// <c>sound_request.proto</c>, поэтому разбор на клиенте сводится к выбору
/// ветки.
/// </para>
/// </remarks>
public interface ISoundRequestHub
{
    /// <summary>Текущее состояние плеера: играет, пауза, громкость.</summary>
    Task PlayerStateChange(PlayerStateSnapshot playerStateChange);

    /// <summary>Состав очереди: что добавлено, что удалено, что переехало.</summary>
    Task QueueChanged(QueueState queueChanged);
}
