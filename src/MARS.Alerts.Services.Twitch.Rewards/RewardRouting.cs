using System.Text;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Нормализация названия награды Twitch в локальный ключ обработчика.
/// </summary>
/// <remarks>
/// Exchange у всех наград общий (<c>mars.reward-redeemed</c>), поэтому выбор
/// обработчика идёт по <see cref="RewardRedeemedEvent.RewardTitle"/>.
/// Регистр и все символы кроме букв/цифр отбрасываются: «Phonk Edit» и
/// «phonkedit» дают один ключ.
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
