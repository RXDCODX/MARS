namespace MARS.Shared.Models.Rewards;

/// <summary>
/// Дневные правила наград: в какие дни награда вообще может быть включена.
/// </summary>
/// <remarks>
/// <para>
/// Правила перенесены из монолита один в один: там каждая награда отдавала
/// <c>Func&lt;bool&gt; IsRewardEnabled</c>, и раз в пять минут
/// <c>EnsureRewardStateAsync</c> включал или выключал награду в Twitch. Отказа в
/// редемпшене не было и не должно быть: неправильный день означает, что награда
/// недоступна для редемпшена, а не что поинты вернут после.
/// </para>
/// <para>
/// Функция, а не битовая маска в БД: правила меняются вместе с кодом наград, а
/// не через админку, и должны быть видны в ревью. Если понадобится
/// редактирование из админки, здесь появится чтение колонки — поток событий при
/// этом не меняется.
/// </para>
/// </remarks>
public static class RewardDayRules
{
    private static readonly DayOfWeek[] WeekDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Wednesday,
        DayOfWeek.Friday,
    ];

    private static readonly IReadOnlyDictionary<string, DayOfWeek[]> Rules = new Dictionary<
        string,
        DayOfWeek[]
    >(StringComparer.Ordinal)
    {
        [RewardAlertKeys.MikuMonday] = [DayOfWeek.Monday],
        [RewardAlertKeys.MikuRoll] = [DayOfWeek.Monday],
        [RewardAlertKeys.FumoFridayNight] = [DayOfWeek.Friday],
        [RewardAlertKeys.FumoRoll] = [DayOfWeek.Friday],
        [RewardAlertKeys.WednsdayFrog] = [DayOfWeek.Wednesday],
        [RewardAlertKeys.FrogRoll] = [DayOfWeek.Wednesday],
        [RewardAlertKeys.WaifuAdd] = WeekDays,
        [RewardAlertKeys.WaifuMerge] = WeekDays,
        [RewardAlertKeys.WaifuShow] = WeekDays,
        [RewardAlertKeys.WaifuRoll] = WeekDays,
        // В монолите это правило было `() => false`: награда выключена всегда.
        [RewardAlertKeys.AudioQuiz] = [],
    };

    /// <summary>Дни, в которые алерт разрешён. <c>null</c> — ограничения нет.</summary>
    public static IReadOnlyList<DayOfWeek>? DaysFor(string? alertKey)
    {
        IReadOnlyList<DayOfWeek>? result = null;

        if (!string.IsNullOrWhiteSpace(alertKey) && Rules.TryGetValue(alertKey, out var days))
        {
            result = days;
        }

        return result;
    }

    /// <summary>Разрешён ли алерт сегодня (локальная дата машины).</summary>
    public static bool IsAllowedToday(string? alertKey)
    {
        return IsAllowedOn(alertKey, DateTime.Now);
    }

    /// <summary>
    /// Разрешён ли алерт в указанный момент. Вынесено отдельно, чтобы тест
    /// проверял все семь дней без ожидания настоящего дня недели.
    /// </summary>
    public static bool IsAllowedOn(string? alertKey, DateTime localMoment)
    {
        var days = DaysFor(alertKey);
        var result = days is null || days.Contains(localMoment.DayOfWeek);

        return result;
    }
}
