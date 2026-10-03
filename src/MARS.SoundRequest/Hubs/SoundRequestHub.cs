using MARS.Shared.Grpc.SoundRequest;
using MARS.SoundRequest.Hubs.Dtos;
using MARS.SoundRequest.Hubs.Interfaces;
using MARS.SoundRequest.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MARS.SoundRequest.Hubs;

/// <summary>
/// Хаб звуковых запросов.
/// </summary>
/// <remarks>
/// <para>
/// Устроен так же, как хабы оверлея и трека: своего состояния нет, всё
/// приходит из широковещателя через реле.
/// </para>
/// <para>
/// Свою долгую жизнь методов не имеет: браузер шлёт <c>SkipTrack</c>,
/// <c>PlayPrevious</c>, <c>FrontStateChange</c>, <c>TrackProgress</c>,
/// <c>Started</c>, <c>Ended</c> и <c>ErrorPlaying</c>, а реализованы они были
/// только в gRPC-сервисе. Пока хаб был классом без методов, пульт плеера и
/// видеоэкран не делали ничего: каждое нажатие давало «Method does not exist»,
/// а экраны выглядели живыми, потому что состояние приходило по REST.
/// </para>
/// <para>
/// Тела команд лежат в <c>ISoundRequestPlayback</c>, а не здесь и не в
/// gRPC-сервисе: оба транспорта вызывают один и тот же код, иначе они
/// разошлись бы там, где это заметят только на стенде.
/// </para>
/// </remarks>
public class SoundRequestHub(ISoundRequestPlayback players) : Hub<ISoundRequestHub>
{
    /// <summary>
    /// Общие команды клиента.
    /// </summary>
    /// <remarks>
    /// Через конструктор, а не свойство: хаб создаётся контейнером на каждый
    /// вызов, и внедряемое свойство осталось бы <c>null</c> — ошибка уехала бы
    /// в обработчик метода, а не в падение при создании.
    /// </remarks>
    public ISoundRequestPlayback Players { get; } = players;

    /// <summary>Фронтенд сообщает, что трек начал воспроизводиться.</summary>
    public Task Started(TrackInfoHubDto track, CancellationToken cancellationToken) =>
        Players.StartedAsync(track, cancellationToken);

    /// <summary>Фронтенд сообщает, что трек завершился.</summary>
    public Task Ended(TrackInfoHubDto track, CancellationToken cancellationToken) =>
        Players.EndedAsync(track, cancellationToken);

    /// <summary>Фронтенд сообщает об ошибке воспроизведения.</summary>
    public Task ErrorPlaying(TrackInfoHubDto track, CancellationToken cancellationToken) =>
        Players.ErrorPlayingAsync(track, cancellationToken);

    /// <summary>Фронтенд сообщает об изменении состояния плеера.</summary>
    public Task FrontStateChange(PlayerStateHubDto state, CancellationToken cancellationToken) =>
        Players.FrontStateChangeAsync(state, cancellationToken);

    /// <summary>Фронтенд сообщает о прогрессе воспроизведения.</summary>
    public Task TrackProgress(double seconds, CancellationToken cancellationToken) =>
        Players.TrackProgressAsync(seconds, null, cancellationToken);

    /// <summary>Переключение на следующий трек.</summary>
    public Task SkipTrack(CancellationToken cancellationToken) =>
        Players.SkipAsync(cancellationToken);

    /// <summary>Возврат к предыдущему треку из истории.</summary>
    public Task PlayPrevious(CancellationToken cancellationToken) =>
        Players.PlayPreviousAsync(cancellationToken);
}
