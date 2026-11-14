using MARS.Alerts.Application.Twitch.Rewards.Models;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardGaoAlert</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class GaoAlertHandler(ILogger<GaoAlertHandler> logger, RickRollerService rickRollerService)
    : RewardHandlerBase
{
    public override string RewardKey => "gaoalert";

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        var text = rewardEvent.UserInput?.Trim() ?? string.Empty;
        var isJustText = text.Contains(' ');

        GaoAlertDto gaoAlert;

        if (!isJustText)
        {
            var candidate = text.StartsWith('@') ? text[1..] : text;
            var isValidTwitchUsername = candidate.Length <= 25 && IsValidTwitchUsername(candidate);

            if (isValidTwitchUsername)
            {
                gaoAlert = new GaoAlertDto
                {
                    TwitchUser = new { DisplayName = candidate },
                    IsJustText = false,
                };

                if (user is not null)
                {
                    await rickRollerService.TryRickRollAsync(user, () => Task.CompletedTask);
                }
                else
                {
                    //   await notifier.GaoAlert(gaoAlert);
                }

                logger.LogInformation("Gao alert with user {UserName}", candidate);
                return;
            }
        }

        gaoAlert = new GaoAlertDto { IsJustText = true, JustText = text };

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => Task.CompletedTask);
        }
        else
        {
            // await notifier.GaoAlert(gaoAlert);
        }

        logger.LogInformation("Gao alert with text {Text}", text);
    }

    private static bool IsValidTwitchUsername(string value) =>
        System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z0-9_]+$");
}
