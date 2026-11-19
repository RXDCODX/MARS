namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на выдачу VIP-статуса пользователю канала Twitch.
/// Публикуют сервисы без Helix-подключения; обрабатывает MARS.Twitch.Api.
/// </summary>
public sealed class TwitchVipGrantEvent : IMarsEventMessage
{
    public string UserId { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
}
