using System.Drawing;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._160_LegBum;

/// <summary>
/// Обработчик reward-события <c>RewardLegBum</c>: возврат баллов, если ввод
/// содержит «asp/асп».
/// </summary>
public class LegBumHandler(
    ILogger<LegBumHandler> logger,
    ITwitchInternalClient twitchClient,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 160;
    public override string AlertDisplayName { get; set; } = "발 НОГОЙ БОМЖА";
    public override string AlertDescription { get; set; } =
        "🦵 Возращает баллы за использование если топтать asp/асп'a (аспиранта)";
    public override Color Color { get; set; } = Color.FromArgb(255, 0, 0);
    public override int Cost { get; init; } = 160;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled)
        );
        return Task.CompletedTask;
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var input = rewardEvent.UserInput ?? string.Empty;
        if (
            !input.Contains("asp", StringComparison.OrdinalIgnoreCase)
            && !input.Contains("асп", StringComparison.OrdinalIgnoreCase)
        )
        {
            logger.LogInformation(
                "LegBum: no refund keyword in input from {UserName}",
                rewardEvent.UserName
            );
            return;
        }

        var user = rewardEvent.User;
        if (
            user is null
            || string.IsNullOrWhiteSpace(rewardEvent.RewardId)
            || string.IsNullOrWhiteSpace(rewardEvent.MessageId)
        )
        {
            return;
        }

        await twitchClient.RefundRewardAsync(
            new RewardRefundRequest
            {
                RewardId = rewardEvent.RewardId,
                RedemptionIds = [rewardEvent.MessageId],
                Reason = "LegBum: asp keyword",
            }
        );

        await twitchClient.SendChatMessageAsync(
            new TwitchChatSendRequest
            {
                Message =
                    $"@{rewardEvent.UserName}, твои {rewardEvent.Cost} баллов были возвращены!",
            }
        );

        logger.LogInformation("LegBum: refunded redemption for {UserName}", rewardEvent.UserName);
    }
}
