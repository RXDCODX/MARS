namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Запрос на отправку сообщения в канал Discord.
/// </summary>
public sealed class DiscordMessageSendRequest
{
    public ulong ChannelId { get; set; }

    public string Message { get; set; } = string.Empty;
}
