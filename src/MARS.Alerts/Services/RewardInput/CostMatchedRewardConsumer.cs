using MARS.Shared.Configuration;
using MARS.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace MARS.Alerts.Services.RewardInput;

/// <summary>
/// Потребитель погашений наград без текста пользователя: показывает алерт,
/// у которого заявлена та же цена в баллах канала.
/// </summary>
/// <remarks>
/// Очередь <c>alerts.costmatch</c> с биндингом ровно на
/// <see cref="RabbitMqConfig.RewardRedeemed"/>: <c>TwitchMediaAlerts</c> на
/// <c>twitch.reward.#</c> этот ключ уже разбирает, но по привязке награды к
/// медиа, а не по цене.
/// </remarks>
public class CostMatchedRewardConsumer(
    IOptions<RabbitMqOptions> options,
    RewardBoundAlertDispatcher dispatcher,
    ILogger<CostMatchedRewardConsumer> logger
)
    : RabbitMqConsumerBase(
        options.Value,
        "alerts",
        QueueName,
        [RabbitMqConfig.RewardRedeemed],
        logger
    )
{
    public const string QueueName = "alerts.costmatch";

    protected override async Task HandleMessageAsync(
        string routingKey,
        string json,
        CancellationToken ct
    )
    {
        var rewardEvent = Deserialize<RewardRedeemedEvent>(json);

        if (rewardEvent is null)
        {
            return;
        }

        var user =
            rewardEvent.User
            ?? new MARS.Shared.Models.TwitchUser
            {
                TwitchId = rewardEvent.UserId,
                UserLogin = rewardEvent.UserName,
                DisplayName = rewardEvent.UserName,
            };

        await dispatcher.DispatchRedemptionAsync(rewardEvent, user, ct);
    }
}
