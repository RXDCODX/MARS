using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.TwitchRewards._160_LegBum;

/// <summary>
/// Обработчик reward-события <c>RewardLegBum</c>: возврат баллов, если ввод
/// содержит «asp/асп».
/// </summary>
public class LegBumHandler(
    ILogger<LegBumHandler> logger,
    IMessageBus messageBus,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override string RewardCode => "leg_bum";
    public override int RewardCost { get; } = 160;
    public override string AlertDisplayName { get; set; } = "발 НОГОЙ БОМЖА";
    public override string AlertDescription { get; set; } =
        "🦵 Возращает баллы за использование если топтать asp/асп&#39;a (аспиранта)";
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

        await messageBus.PublishAsync(
            new RewardRefundEvent
            {
                RewardId = rewardEvent.RewardId,
                RedemptionIds = [rewardEvent.MessageId],
                Reason = "LegBum: asp keyword",
            }
        );

        await messageBus.PublishAsync(
            new ChatSendEvent
            {
                Message =
                    $"@{rewardEvent.UserName}, твои {rewardEvent.Cost} баллов были возвращены!",
            }
        );

        logger.LogInformation("LegBum: refunded redemption for {UserName}", rewardEvent.UserName);
    }
}