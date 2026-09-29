using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardAdhdSuperpower</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class AdhdSuperpowerHandler(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    ILogger<AdhdSuperpowerHandler> logger,
    RickRollerService rickRollerService
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardAdhdSuperpower;

    private const int AdhdDurationSeconds = 60;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                () => hubContext.Clients.All.Adhd(AdhdDurationSeconds)
            );
        }
        else
        {
            await hubContext.Clients.All.Adhd(AdhdDurationSeconds);
        }

        logger.LogInformation(
            "ADHD activated by {UserName} for {Duration} seconds",
            rewardEvent.UserName,
            AdhdDurationSeconds
        );
    }
}
