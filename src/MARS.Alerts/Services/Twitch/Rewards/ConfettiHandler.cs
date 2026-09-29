using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Alerts.Models;
using MARS.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardConfetti</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class ConfettiHandler(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardConfetti;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => hubContext.Clients.All.MakeScreenParticles(TwitchScreenParticles.Confetty)
            );
        }
        else
        {
            await hubContext.Clients.All.MakeScreenParticles(TwitchScreenParticles.Confetty);
        }
    }
}
