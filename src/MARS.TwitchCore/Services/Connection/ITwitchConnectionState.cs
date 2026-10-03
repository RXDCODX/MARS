namespace MARS.TwitchCore.Services.Connection;

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
}
