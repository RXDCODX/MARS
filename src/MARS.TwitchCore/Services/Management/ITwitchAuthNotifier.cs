namespace MARS.TwitchCore.Services.Management;

/// <summary>
/// Уведомление о том, что сервису нужен новый токен Twitch.
/// </summary>
/// <remarks>
/// Отдельный интерфейс — потому что Telegram может быть не настроен вовсе, а
/// сервис должен стартовать в обоих случаях: без нотификатора не роняет хост,
/// с ним — сообщает владельцу ссылку на переавторизацию.
/// </remarks>
public interface ITwitchAuthNotifier
{
    /// <summary>
    /// Сообщает, что токена нет и его надо получить по ссылке.
    /// </summary>
    Task NotifyMissingTokenAsync();
}
