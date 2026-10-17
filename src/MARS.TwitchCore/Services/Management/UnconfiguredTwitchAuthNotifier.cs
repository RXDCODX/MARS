using Microsoft.Extensions.Logging;

namespace MARS.TwitchCore.Services.Management;

/// <summary>
/// Заглушка нотификатора на случай, когда Telegram не настроен.
/// </summary>
/// <remarks>
/// Молчание here было бы хуже отсутствия уведомления: владелец видит, что сервис
/// просит токен в логе, и не понимает, почему сообщения не пришло. Поэтому
/// говорит в лог, чего именно не хватает.
/// </remarks>
public sealed class UnconfiguredTwitchAuthNotifier(ILogger<UnconfiguredTwitchAuthNotifier> logger)
    : ITwitchAuthNotifier
{
    /// <inheritdoc />
    public Task NotifyMissingTokenAsync()
    {
        logger.LogWarning(
            "Уведомление о переавторизации Twitch не отправлено: Telegram не настроен "
                + "(задайте Telegram__BotToken и Telegram__AdminIds__0)."
        );

        return Task.CompletedTask;
    }
}
