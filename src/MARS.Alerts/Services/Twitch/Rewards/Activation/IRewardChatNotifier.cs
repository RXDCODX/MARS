namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Сообщение в чат Twitch от имени бота.
/// </summary>
/// <remarks>
/// MARS.Alerts не держит IRC-соединения: единственный владелец — MARS.TwitchCore,
/// иначе Twitch закрывает более старое из двух подключений. Поэтому сервис
/// публикует <c>chat.send</c>, а не отправляет сообщение сам.
/// </remarks>
public interface IRewardChatNotifier
{
    Task SendAsync(
        string message,
        string? replyTo = null,
        CancellationToken cancellationToken = default
    );
}
