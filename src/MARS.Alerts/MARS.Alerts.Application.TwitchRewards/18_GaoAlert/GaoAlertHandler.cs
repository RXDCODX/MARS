using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Models;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._18_GaoAlert;

/// <summary>
/// Обработчик reward-события <c>RewardGaoAlert</c>.
/// </summary>
public class GaoAlertHandler(
    ILogger<GaoAlertHandler> logger,
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
) : RewardHandlerBase
{
    public override string RewardCode => "gao_alert";

    private readonly EventBroadcaster<TelegramusEvent> _broadcaster = broadcaster;

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        var text = (rewardEvent.UserInput ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("GaoAlert: empty input from {UserName}", rewardEvent.UserName);
            return;
        }

        var normalized = text.TrimStart('@');

        if (!IsValidTwitchUsername(normalized))
        {
            var dto = new GaoAlertDto { IsJustText = true, JustText = normalized };
            await PublishAsync(dto, rewardEvent);
            return;
        }

        var candidate = normalized;
        var dto2 = new GaoAlertDto
        {
            TwitchUser = new { DisplayName = candidate },
            IsJustText = false,
        };
        await PublishAsync(dto2, rewardEvent);
    }

    private async Task PublishAsync(GaoAlertDto dto, RewardRedeemedEvent rewardEvent)
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
                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        GaoAlert = new GaoAlertEvent
                        {
                            GaoAlertJson = JsonSerializer.Serialize(dto),
                        },
                    }
                );
            }
        );
    }

    private static bool IsValidTwitchUsername(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 25
        && value.All(c => char.IsLetterOrDigit(c) || c == '_');
}
