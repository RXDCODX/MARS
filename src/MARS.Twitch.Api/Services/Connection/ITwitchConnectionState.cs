namespace MARS.Twitch.Api.Services.Connection;

/// <summary>
/// Состояние подключения к чату Twitch.
///
/// Интерфейс нужен там, где требуется только факт подключения, а не сам
/// менеджер: у <see cref="TwitchConnectionManager"/> конструктор создаёт клиента
/// TwitchLib, и подменить его в проверке нельзя. Пример — контроллер статистики,
/// которому достаточно знать, подключён ли чат.
/// </summary>
public interface ITwitchConnectionState
{
    bool IsConnected { get; }

    /// <summary>Список каналов, в которые зашёл бот (через запятую).</summary>
    string JoinedChannels { get; }

    /// <summary>Время последнего успешного подключения.</summary>
    DateTime? LastConnectedAt { get; }

    /// <summary>Время последнего отключения.</summary>
    DateTime? LastDisconnectedAt { get; }

    /// <summary>Причина последнего отключения (если известна).</summary>
    string? LastDisconnectReason { get; }

    /// <summary>Количество попыток переподключения.</summary>
    int ReconnectAttempts { get; }

    /// <summary>Идёт ли сейчас переподключение.</summary>
    bool IsReconnecting { get; }
}
