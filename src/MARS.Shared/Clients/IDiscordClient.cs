namespace MARS.Shared.Clients;

/// <summary>
/// Владелец единственного подключения к Discord — MARS.Discord. Остальные
/// сервисы отправляют сообщения в канал через его внутренний API, а не
/// заводят собственное подключение.
/// </summary>
public interface IDiscordClient
{
    /// <summary>
    /// Отправляет сообщение в канал Discord. false — Discord недоступен или
    /// отклонил отправку; причина уже в логе сервиса-владельца.
    /// </summary>
    Task<bool> SendMessageAsync(
        ulong channelId,
        string message,
        CancellationToken cancellationToken = default
    );
}
