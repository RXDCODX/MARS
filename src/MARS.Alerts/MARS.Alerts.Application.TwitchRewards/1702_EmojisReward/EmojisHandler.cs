using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.Twitch.Rewards._1702_EmojisReward;

/// <summary>
/// Обработчик reward-события <c>RewardEmojis</c>.
/// </summary>
public class EmojisHandler : RewardHandlerBase
{
    private readonly ILogger<EmojisHandler> _logger;
    private readonly RickRollerService _rickRollerService;
    private readonly EventBroadcaster<TelegramusEvent> _broadcaster;

    public EmojisHandler(
        ILogger<EmojisHandler> logger,
        RickRollerService rickRollerService,
        EventBroadcaster<TelegramusEvent> broadcaster
    )
    {
        _logger = logger;
        _rickRollerService = rickRollerService;
        _broadcaster = broadcaster;

        OnTwitchRewardInput += HandleRewardInputAsync;
    }

    public override string RewardCode => "emojis";

    private Task HandleRewardInputAsync(ChatEventMessageEvent message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = new TwitchUser
        {
            TwitchId = message.UserId,
            UserLogin = message.UserName,
            DisplayName = message.DisplayName ?? message.UserName,
            ChatColor = message.ChatColor,
        };

        _ = _rickRollerService.TryRickRollAsync(
            user,
            async () =>
            {
                await _broadcaster.BroadcastAsync(
                    new TelegramusEvent
                    {
                        MakeScreenEmojisParticles = new ScreenEmojisParticlesEvent
                        {
                            MessageJson = JsonSerializer.Serialize(message.Message),
                        },
                    }
                );
            }
        );

        _logger.LogInformation("Emojis reward activated by {UserName}", message.UserName);
        return Task.CompletedTask;
    }

    protected override Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    ) => Task.CompletedTask;
}
