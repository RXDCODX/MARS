namespace MARS.SoundRequest.Hubs.Dtos;

/// <summary>
/// Состояние плеера в форме, которую шлёт браузер.
/// </summary>
/// <remarks>
/// <para>
/// Не <c>PlayerState</c> из <c>MARS.SoundRequest.Entities</c> и не снимок из
/// proto: это форма REST-контракта, потому что клиент присылает её в
/// <c>FrontStateChange</c>. Отличие видно на прогрессе — здесь он строка
/// <c>hh:mm:ss</c>, тогда как в proto это целые секунды.
/// </para>
/// <para>
/// Перечисления приходят именами: <c>JsonStringEnumConverter</c> добавлен в
/// <c>AddMarsSignalR</c> специально для хабов, настройка MVC на них не действует.
/// </para>
/// </remarks>
public class PlayerStateHubDto
{
    public string Id { get; set; } = string.Empty;

    public string? CurrentQueueItemId { get; set; }

    /// <summary>Прогресс в формате <c>hh:mm:ss</c>.</summary>
    public string? CurrentTrackProgress { get; set; }

    public PlayerStateStateEnum State { get; set; }

    public PlayerStateVideoStateEnum VideoState { get; set; }

    public bool IsMuted { get; set; }

    public bool PausedByMute { get; set; }

    public float Volume { get; set; }

    public QueueItemHubDto? CurrentQueueItem { get; set; }
}
