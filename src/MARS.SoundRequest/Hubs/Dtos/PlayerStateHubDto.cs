namespace MARS.SoundRequest.Hubs.Dtos;

/// <summary>
/// Состояние плеера в форме, которую шлёт браузер.
/// </summary>
/// <remarks>
/// <para>
/// Не <c>PlayerState</c> из <c>MARS.SoundRequest.Entities</c> и не снимок из
/// proto: это форма REST-контракта, потому что клиент уже присылает её в
/// <c>FrontStateChange</c>. Отличие видно на прогрессе — здесь он строка
/// <c>hh:mm:ss</c>, тогда как в proto это целые секунды.
/// </para>
/// <para>
/// Перечисления приходят именами: настройка <c>JsonStringEnumConverter</c>
/// добавлена в <c>AddMarsSignalR</c> специально для хабов, настройка MVC на них
/// не действует.
/// </para>
/// </remarks>
public class PlayerStateHubDto
{
    public string Id { get; set; } = string.Empty;

    public string StateVersion { get; set; } = string.Empty;

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

/// <summary>Состояние плеера в терминах клиента.</summary>
public enum PlayerStateStateEnum
{
    Stopped = 0,
    Playing = 1,
    Paused = 2,
}

/// <summary>Режим видео в терминах клиента.</summary>
public enum PlayerStateVideoStateEnum
{
    Hidden = 0,
    Video = 1,
}

/// <summary>Элемент очереди в форме REST-контракта.</summary>
public class QueueItemHubDto
{
    public string Id { get; set; } = string.Empty;

    public TrackInfoHubDto? Track { get; set; }
}

/// <summary>
/// Трек в форме REST-контракта.
/// </summary>
/// <remarks>
/// Повторяет <c>BaseTrackInfo</c> клиента, а не proto-сообщение: длительность
/// приходит строкой <c>hh:mm:ss</c>, а не целыми секундами. Сокращённый DTO
/// терял длительность и авторов, и реле доставляло событие окончания с пустым
/// треком.
/// </remarks>
public class TrackInfoHubDto
{
    public string Id { get; set; } = string.Empty;

    public string TrackName { get; set; } = string.Empty;

    public string[]? Authors { get; set; }

    /// <summary>Длительность в формате <c>hh:mm:ss</c>.</summary>
    public string? Duration { get; set; }

    public string? Url { get; set; }

    public string? ArtworkUrl { get; set; }

    public string? VideoId { get; set; }
}
