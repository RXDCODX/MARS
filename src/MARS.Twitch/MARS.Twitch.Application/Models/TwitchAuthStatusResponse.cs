namespace MARS.Twitch.Application.Models;

/// <summary>
/// Статус авторизации и подключения Twitch: состояние чата, EventSub и токена.
/// </summary>
public class TwitchAuthStatusResponse
{
    /// <summary>Подключён ли чат (IRC/WebSocket) TwitchLib.</summary>
    public bool IsChatConnected { get; set; }

    /// <summary>Подключён ли EventSub WebSocket.</summary>
    public bool IsEventSubConnected { get; set; }

    /// <summary>Список каналов, в которые зашёл бот (через запятую).</summary>
    public string JoinedChannels { get; set; } = string.Empty;

    /// <summary>Время последнего успешного подключения.</summary>
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>Время последнего отключения.</summary>
    public DateTime? LastDisconnectedAt { get; set; }

    /// <summary>Причина последнего отключения (если известна).</summary>
    public string? LastDisconnectReason { get; set; }

    /// <summary>Количество попыток переподключения.</summary>
    public int ReconnectAttempts { get; set; }

    /// <summary>Идёт ли сейчас переподключение.</summary>
    public bool IsReconnecting { get; set; }

    /// <summary>Есть ли токен в БД.</summary>
    public bool HasToken { get; set; }

    /// <summary>Когда токен был создан/обновлён.</summary>
    public DateTime? TokenWhenCreated { get; set; }

    /// <summary>Срок действия токена (если известен).</summary>
    public TimeSpan? TokenExpiresIn { get; set; }

    /// <summary>Истёк ли токен.</summary>
    public bool IsTokenExpired { get; set; }
}
