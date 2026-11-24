using System.Text;
using MARS.Alerts.Application.TwitchRewards.Routing;

namespace MARS.Alerts.Application.TwitchRewards.Routing;

/// <summary>
/// Нормализация названия награды Twitch в ключ (нижний регистр, только буквы и цифры).
/// </summary>
/// <remarks>
/// В текущей архитектуре маршрутизация редемпшенов идёт по стоимости
/// (<see cref="RewardHandlerBase.RewardCost"/>): менеджер рассылает событие
/// всем подписчикам, а каждый обработчик сам отсекает чужие стоимости.
/// Этот helper остаётся для совместимости и тестов нормализации имён.
/// </remarks>
public static class RewardRouting
{
    public static string KeyFor(string? rewardTitle)
    {
        if (string.IsNullOrWhiteSpace(rewardTitle))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(rewardTitle.Length);

        foreach (var symbol in rewardTitle)
        {
            if (char.IsLetterOrDigit(symbol))
            {
                normalized.Append(char.ToLowerInvariant(symbol));
            }
        }

        return normalized.ToString();
    }
}
