using MARS.Shared.Models.Rewards;
using MARS.TwitchCore.Entities;

namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Решает, должна ли награда быть включена в Twitch прямо сейчас.
/// </summary>
/// <remarks>
/// <para>
/// В монолите это была <c>Func&lt;bool&gt; IsRewardEnabled</c> у каждой награды:
/// раз в пять минут <c>EnsureRewardStateAsync</c> включал или выключал награду,
/// поэтому редемпшн в неправильный день был невозможен, а поинты никому не
/// возвращали. Здесь та же логика, но правило берётся по
/// <see cref="ChannelRewardRecord.AlertKey"/>.
/// </para>
/// <para>
/// Функция вынесена отдельно и принимает момент времени, чтобы тест проверял все
/// семь дней недели без ожидания настоящего дня.
/// </para>
/// </remarks>
public static class RewardAvailability
{
    /// <summary>
    /// Награда включена, если не выключена вручную и день недели разрешён
    /// (отсутствие правила означает «любой день»).
    /// </summary>
    public static bool IsEnabled(ChannelRewardRecord record, DateTime localMoment)
    {
        var result =
            record.IsAlertEnabled && RewardDayRules.IsAllowedOn(record.AlertKey, localMoment);

        return result;
    }

    /// <summary>То же для текущего момента.</summary>
    public static bool IsEnabled(ChannelRewardRecord record)
    {
        var result = IsEnabled(record, DateTime.Now);

        return result;
    }
}
