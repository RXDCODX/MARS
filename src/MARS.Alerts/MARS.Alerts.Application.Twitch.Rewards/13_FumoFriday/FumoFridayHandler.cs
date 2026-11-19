using System.Collections.Concurrent;
using Google.Protobuf;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards._13_FumoFriday;

/// <summary>
/// Обработчик reward-события <c>RewardFumoFriday</c>.
/// </summary>
public class FumoFridayHandler(
    ILogger<FumoFridayHandler> logger,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "fumo_friday";

    public override IRewardActivationPolicy Activation => new DayOfWeekPolicy(DayOfWeek.Friday);

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _shown = new();

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;
        if (user is null)
        {
            return;
        }

        await rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                _shown[user.TwitchId] = DateTimeOffset.UtcNow;

                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        FumoFriday = new FumoFridayEvent
                        {
                            DisplayName = user.DisplayName,
                            Color = string.IsNullOrWhiteSpace(user.ChatColor)
                                ? "#ffffff"
                                : user.ChatColor,
                        },
                    }
                );
            }
        );

        logger.LogInformation("FumoFriday activated by {UserName}", rewardEvent.UserName);
    }
}
