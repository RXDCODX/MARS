using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Alerts.Models;
using MARS.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardGaoAlert</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class GaoAlertHandler(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    ILogger<GaoAlertHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardGaoAlert;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
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
                    await rickRollerService.TryRickRollAsync(
                        user,
                        () => hubContext.Clients.All.GaoAlert(gaoAlert)
                    );
                }
                else
                {
                    await hubContext.Clients.All.GaoAlert(gaoAlert);
                }

                logger.LogInformation("Gao alert with user {UserName}", candidate);
                return;
            }
        }

        gaoAlert = new GaoAlertDto { IsJustText = true, JustText = text };

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => hubContext.Clients.All.GaoAlert(gaoAlert)
            );
        }
        else
        {
            await hubContext.Clients.All.GaoAlert(gaoAlert);
        }

        logger.LogInformation("Gao alert with text {Text}", text);
    }

    private static bool IsValidTwitchUsername(string value) =>
        System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-zA-Z0-9_]+$");
}
