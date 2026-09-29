namespace MARS.WaifuGacha.Entities;

/// <summary>
/// Ключи <c>waifu.RootState</c> — конфигурация, которой владеет MARS.WaifuGacha.
/// Раньше эти ключи заводил только MARS.Admin в своей схеме <c>admin</c>, поэтому
/// WaifuGacha их никогда не видел и всегда возвращал зашитые 20 минут.
/// </summary>
public static class RootStateKeys
{
    public const string WaifuRollCooldownMinutes = "WaifuRollCooldownMinutes";
    public const string FumoRollCooldownMinutes = "FumoRollCooldownMinutes";
    public const string MikuRollCooldownMinutes = "MikuRollCooldownMinutes";
    public const string FrogRollCooldownMinutes = "FrogRollCooldownMinutes";

    /// <summary>Имя ролла, как его знает <c>RollCooldowns.RollType</c>.</summary>
    public const string WaifuRollType = "Waifu";

    public const string FumoRollType = "Fumo";
    public const string MikuRollType = "Miku";
    public const string FrogRollType = "Frog";

    /// <summary>
    /// Все ключи, которые сервис создаёт при старте, с описаниями и значениями по умолчанию.
    /// </summary>
    public static IReadOnlyDictionary<string, (string Value, string Description)> Defaults { get; } =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            [WaifuRollCooldownMinutes] = ("20", "Кулдаун ролла вайфу в минутах"),
            [FumoRollCooldownMinutes] = ("20", "Кулдаун ролла фумо в минутах"),
            [MikuRollCooldownMinutes] = ("20", "Кулдаун ролла мику в минутах"),
            [FrogRollCooldownMinutes] = ("20", "Кулдаун ролла жабы в минутах"),
        };

    /// <summary>
    /// Ключ кулдауна для типа ролла. Неизвестные типы трактуются как ролл вайфу,
    /// чтобы новая разновидность ролла не ломала существующий дефолт.
    /// </summary>
    public static string RollCooldownKey(string rollType)
    {
        var result = rollType?.Trim().ToLowerInvariant() switch
        {
            "fumo" => FumoRollCooldownMinutes,
            "miku" => MikuRollCooldownMinutes,
            "frog" => FrogRollCooldownMinutes,
            _ => WaifuRollCooldownMinutes,
        };

        return result;
    }
}
