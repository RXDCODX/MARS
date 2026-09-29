namespace MARS.TwitchCore.Services.HelloVideos;

/// <summary>
/// Правила eligibility для ежедневного hello-video.
/// Вынесено отдельно, чтобы логику можно было покрыть тестами без БД и Twitch.
/// </summary>
public static class HelloVideoEligibility
{
    /// <summary>
    /// Аудит №17: сравнение <c>LastTimeNotif.Day != now.Day</c> учитывало только
    /// номер дня в месяце, поэтому 1-го числа каждого месяца приветствие
    /// подавлялось. Сравниваем полную дату (год/месяц/день).
    /// </summary>
    public static bool ShouldNotify(DateTime lastNotifiedAt, DateTime now)
    {
        var lastDate = lastNotifiedAt.Date;
        var currentDate = now.Date;

        // Уведомляем, если это не тот же календарный день. Ветка lastDate > currentDate
        // (часы разъехались или LastTimeNotif из будущего) тоже даёт true: иначе
        // пользователь блокировался бы на дни вперёд.
        return lastDate != currentDate;
    }
}
