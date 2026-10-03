using MARS.SoundRequest.Hubs.Dtos;

namespace MARS.SoundRequest.Services.Interfaces;

/// <summary>
/// Команды клиента очереди звуковых запросов.
/// </summary>
/// <remarks>
/// <para>
/// Вынесено из <c>SoundRequestGrpcService</c>, потому что одни и те же семь
/// команд нужны двум транспортам: gRPC для сервисов и SignalR для браузера.
/// Пока логика жила в gRPC-сервисе, браузеру она была недоступна — он до gRPC
/// не ходит, а хаб был классом без единого метода, и кнопки пульта не делали
/// ничего.
/// </para>
/// <para>
/// Дублировать тела в двух местах нельзя: разошлись бы «состояние плеера
/// обновили» и «переключили трек», и расхождение было бы видно только на стенде.
/// </para>
/// </remarks>
public interface ISoundRequestPlayback
{
    /// <summary>Фронтенд сообщает, что трек начал воспроизводиться.</summary>
    Task StartedAsync(TrackInfoHubDto track, CancellationToken cancellationToken);

    /// <summary>Фронтенд сообщает, что трек завершился.</summary>
    Task EndedAsync(TrackInfoHubDto track, CancellationToken cancellationToken);

    /// <summary>Фронтенд сообщает об ошибке воспроизведения.</summary>
    Task ErrorPlayingAsync(TrackInfoHubDto track, CancellationToken cancellationToken);

    /// <summary>Фронтенд сообщает об изменении состояния плеера.</summary>
    Task FrontStateChangeAsync(PlayerStateHubDto state, CancellationToken cancellationToken);

    /// <summary>Фронтенд сообщает о прогрессе воспроизведения.</summary>
    Task TrackProgressAsync(
        double seconds,
        string? excludeSubscriberId,
        CancellationToken cancellationToken
    );

    /// <summary>Переключение на следующий трек.</summary>
    Task SkipAsync(CancellationToken cancellationToken);

    /// <summary>Возврат к предыдущему треку из истории.</summary>
    Task PlayPreviousAsync(CancellationToken cancellationToken);
}
