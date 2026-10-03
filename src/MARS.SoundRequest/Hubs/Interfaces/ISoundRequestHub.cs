using MARS.Shared.Grpc.SoundRequest;

namespace MARS.SoundRequest.Hubs.Interfaces;

/// <summary>
/// Методы хаба звуковых запросов, которые сервер шлёт подписчикам.
/// </summary>
/// <remarks>
/// <para>
/// Хаба на сервере не было, хотя <c>MARS.SoundRequest</c> уже держал
/// <c>GrpcEventBroadcaster&lt;SoundRequestEvent&gt;</c> и gRPC-сервис с той же
/// полезной нагрузкой. Клиент подписывался на <c>hubs/soundrequest</c> и
/// получал 405: путь не был объявлен в Gateway, запрос уходил в catch-all
/// клиента, а nginx отвечал на него отказом по методу.
/// </para>
/// <para>
/// Имена методов совпадают с ветками <c>oneof event</c> в
/// <c>sound_request.proto</c>, поэтому разбор на клиенте сводится к выбору
/// ветки.
/// </para>
/// <para>
/// Интерфейс описывает только направление сервер → клиент. Клиентские команды
/// (<c>SkipTrack</c>, <c>PlayPrevious</c>, <c>FrontStateChange</c> и прочие)
/// объявлены методами самого <c>SoundRequestHub</c>: в интерфейс они попали бы
/// с обратной семантикой, и проверка «каждому методу соответствует событие
/// oneof» перестала бы отличать серверное событие от клиентского вызова.
/// </para>
/// </remarks>
public interface ISoundRequestHub
{
    /// <summary>Текущее состояние плеера: играет, пауза, громкость.</summary>
    Task PlayerStateChange(PlayerStateSnapshot playerStateChange);

    /// <summary>Состав очереди: что добавлено, что удалено, что переехало.</summary>
    Task QueueChanged(QueueState queueChanged);
}
