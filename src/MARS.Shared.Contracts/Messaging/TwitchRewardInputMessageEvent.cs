namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Сообщение чата, отправленное через Twitch channel point reward.
/// Отдельный контракт позволяет подписчикам reward-input получать свою копию
/// события, не смешивая его с обычным потоком сообщений чата.
/// </summary>
public sealed class TwitchRewardInputMessageEvent : IMarsEventMessage
{
    public ChatEventMessageEvent Message { get; set; } = new();
}
