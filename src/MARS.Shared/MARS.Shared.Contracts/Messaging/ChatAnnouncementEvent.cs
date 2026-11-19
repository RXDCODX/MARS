namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на отправку объявления в чат Twitch (цветное сообщение).
/// Публикуют сервисы без собственного IRC/Helix-подключения;
/// обрабатывает MARS.Twitch.Api.
/// </summary>
public sealed class ChatAnnouncementEvent : IMarsEventMessage
{
    /// <summary>Канал (по умолчанию основной канал стримера).</summary>
    public string? Channel { get; init; }

    /// <summary>Текст объявления.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Цвет объявления (primary/blue/green/orange/purple).</summary>
    public string Color { get; init; } = "primary";
}
