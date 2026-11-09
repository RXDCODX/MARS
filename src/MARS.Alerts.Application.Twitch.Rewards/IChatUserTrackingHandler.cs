using MARS.Shared.Contracts.Messaging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик потока сообщений чата (<c>twitch.message.received</c>).
/// Нужен эффектам, которые накапливают состав участников чата между активациями
/// (например MIKU MIKU BEAM собирает уникальных зрителей окна).
/// </summary>
public interface IChatUserTrackingHandler
{
    /// <summary>Регистрирует участника чата в окне наблюдения.</summary>
    void TrackChatUser(ChatEventMessageEvent chatEventMessage);
}
