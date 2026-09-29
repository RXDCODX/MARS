using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardCredits</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CreditsHandler(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    ILogger<CreditsHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardCredits;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(user, () => hubContext.Clients.All.Credits());
        }
        else
        {
            await hubContext.Clients.All.Credits();
        }

        logger.LogInformation("Credits activated by {UserName}", rewardEvent.UserName);
    }
}
