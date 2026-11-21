using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards;

/// <summary>
/// Обработчик reward-события <c>RewardCinemaRequest</c>.
/// Разбирается общим <c>RewardAlertConsumer</c> — отдельного AMQP-соединения нет.
/// </summary>
public class CinemaRequestHandler(ILogger<CinemaRequestHandler> logger) : RewardHandlerBase
{
    public override string RewardCode => "cinema_request";

    public override IRewardActivationPolicy Activation => DisabledPolicy.Instance;

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "CinemaRequest reward redeemed by {UserName} with input: {Input}",
            rewardEvent.UserName,
            rewardEvent.UserInput
        );
    }
}
