using MARS.Shared.Messaging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик потока сообщений чата (<c>twitch.message.received</c>).
/// Нужен эффектам, которые накапливают состав участников чата между активациями
/// (например MIKU MIKU BEAM собирает уникальных зрителей окна).
/// </summary>
public interface IChatUserTrackingHandler
{
    /// <summary>Регистрирует участника чата в окне наблюдения.</summary>
    void TrackChatUser(ChatMessageEvent chatMessage);
}
