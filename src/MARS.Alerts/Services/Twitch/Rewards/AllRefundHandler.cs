using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Services.Twitch.Rewards;

/// <summary>
/// Обработчик reward-события <c>RewardAllRefund</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class AllRefundHandler(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    ILogger<AllRefundHandler> logger
) : IRewardAlertHandler
{
    public string RoutingKey => RabbitMqConfig.RewardAllRefund;

    public async Task HandleAsync(RewardRedeemedEvent rewardEvent, CancellationToken ct)
    {
        var user = rewardEvent.User;
        if (user is not null)
        {
            await hubContext.Clients.All.AllRefund(user);
        }

        logger.LogInformation("AllRefund activated by {UserName}", rewardEvent.UserName);
    }
}
