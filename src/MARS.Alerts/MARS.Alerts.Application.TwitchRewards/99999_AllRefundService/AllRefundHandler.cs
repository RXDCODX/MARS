using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.Twitch.Rewards._99999_AllRefundService;

/// <summary>
/// Обработчик reward-события <c>RewardAllRefund</c>.
/// </summary>
public class AllRefundHandler(
    ILogger<AllRefundHandler> logger,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster,
    IMessageBus messageBus,
    AllRefundState state
) : RewardHandlerBase
{
    public override string RewardCode => "all_refund";

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

    private static readonly TimeSpan ActivationDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromMinutes(1);

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var user = rewardEvent.User;

        if (user is not null)
        {
            await rickRollerService.TryRickRollAsync(
                user,
                async () =>
                {
                    await _broadcaster.BroadcastAsync(
                        new TelegramusEvent
                        {
                            AllRefund = new AllRefundEvent
                            {
                                UserJson = JsonSerializer.Serialize(user),
                            },
                        }
                    );

                    await messageBus.PublishAsync(
                        new ChatAnnouncementEvent
                        {
                            Message =
                                $"{user.DisplayName} активировал Алармагеддон! Начинайте тратить баллы через 10 секунд!",
                        }
                    );

                    await Task.Delay(ActivationDelay, ct);

                    state.Activate(ActiveWindow);

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(ActiveWindow);
                        state.Deactivate();
                        await messageBus.PublishAsync(
                            new ChatAnnouncementEvent
                            {
                                Message = "Алармагеддон закончился! Всем спасибо!",
                            }
                        );
                    });
                }
            );
        }

        logger.LogInformation("AllRefund activated by {UserName}", rewardEvent.UserName);
    }
}

