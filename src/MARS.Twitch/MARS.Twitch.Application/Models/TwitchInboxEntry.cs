namespace MARS.Twitch.Application.Models;

/// <summary>
/// Одно сообщение, полученное ботом в чате Twitch.
/// </summary>
/// <remarks>
/// Память, а не база: полный поток сообщений уже уходит в оверлеи через
/// RabbitMQ, а панели нужен только свежий срез для проверки. Хранить историю
/// в БД означало бы миграцию и отдельную таблицу ради консоли.
/// </remarks>
public sealed record TwitchInboxEntry(
    DateTime ReceivedAtUtc,
    string Channel,
    string UserId,
    string Username,
    string DisplayName,
    string Message,
    string BotUsername,
    bool IsModerator,
    bool IsVip,
    bool IsSubscriber,
    bool IsBroadcaster
);
