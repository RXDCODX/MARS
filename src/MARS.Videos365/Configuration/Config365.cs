namespace MARS.Videos365.Configuration;

/// <summary>
/// Учётные данные и адрес сайта-источника для конвейера 365.
/// Секреты (Login/Password) приходят из окружения; в репозитории лежат пустыми.
/// </summary>
public class Config365
{
    public const string SectionName = "Config365";

    public string Site { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public long TelegramChannelId { get; set; }

    /// <summary>
    /// Конвейер не запускается на неполной конфигурации: без логина и пароля
    /// источник вернёт заглушку, а обход дедупликации по <c>SiteId</c> при
    /// пустом ответе приводит к пометке видео «загружено», которого в канале
    /// нет.
    /// </summary>
    public bool IsComplete()
    {
        var result =
            !string.IsNullOrWhiteSpace(Login)
            && !string.IsNullOrWhiteSpace(Password)
            && !string.IsNullOrWhiteSpace(Site)
            && TelegramChannelId != 0;

        return result;
    }
}
