using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardMichaelTime</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class MichaelTimeHandler(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    ILogger<MichaelTimeHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardMichaelTime;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => hubContext.Clients.All.MichaelJackson()
            );
        }
        else
        {
            await hubContext.Clients.All.MichaelJackson();
        }

        logger.LogInformation("MichaelTime activated by {UserName}", rewardEvent.UserName);
    }
}
