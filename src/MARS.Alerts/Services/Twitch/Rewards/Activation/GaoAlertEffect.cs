using System.Text.RegularExpressions;
using MARS.Alerts.Models;
using MARS.Shared.Models.Rewards;

namespace MARS.Alerts.Services.Twitch.Rewards.Activation;

/// <summary>
/// Награда GAO Alert: ввод зрителя — либо ник, которому кричат, либо просто
/// текст.
/// </summary>
/// <remarks>
/// Разбор остался прежним: ник — одно слово из букв, цифр и подчёркиваний не
/// длиннее 25 символов (ограничение Twitch на логин); всё остальное, включая
/// строку с пробелом, показывается как обычный текст. Разбор на сервере, а не
/// на клиенте, потому что от него зависит форма данных, а не оформление.
/// </remarks>
public sealed partial class GaoAlertEffect : IRewardActivationEffect
{
    public string AlertKey => RewardAlertKeys.GaoAlert;

    public Task<RewardEffectResult> ApplyAsync(
        RewardActivationContext context,
        CancellationToken cancellationToken
    )
    {
        var text = context.UserInput?.Trim() ?? string.Empty;
        var candidate = text.StartsWith('@') ? text[1..] : text;
        var isUserName =
            !text.Contains(' ')
            && candidate.Length <= 25
            && TwitchUserNamePattern().IsMatch(candidate);

        GaoAlertDto gaoAlert;

        if (isUserName)
        {
            gaoAlert = new GaoAlertDto
            {
                TwitchUser = new { DisplayName = candidate },
                IsJustText = false,
            };
        }
        else
        {
            gaoAlert = new GaoAlertDto { IsJustText = true, JustText = text };
        }

        return Task.FromResult<RewardEffectResult>(new() { Data = gaoAlert });
    }

    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex TwitchUserNamePattern();
}
