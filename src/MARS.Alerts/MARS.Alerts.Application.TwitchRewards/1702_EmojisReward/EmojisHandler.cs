using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._1702_EmojisReward;

/// <summary>
/// Обработчик reward-события <c>RewardEmojis</c>.
/// </summary>
public class EmojisHandler : RewardHandlerBase
{
    private readonly ITwitchEventValidationService _validator;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IRewardEventManager _rewardEventManager;

    private readonly ILogger<EmojisHandler> _logger;
    private readonly RickRollerService _rickRollerService;
    private readonly IHubContext<AlertsHub, IAlertsHub> _hubContext;

    public EmojisHandler(
        ILogger<EmojisHandler> logger,
        RickRollerService rickRollerService,
        IHubContext<AlertsHub, IAlertsHub> hubContext,
        ITwitchEventValidationService validator,
        IHostApplicationLifetime lifetime,
        IRewardEventManager rewardEventManager
    )
    {
        _validator = validator;
        _lifetime = lifetime;
        _rewardEventManager = rewardEventManager;

        _logger = logger;
        _rickRollerService = rickRollerService;
        _hubContext = hubContext;
    }


    public override int RewardCost { get; } = 1702;
    public override string AlertDisplayName { get; set; } = "😀 Эмодзи!";
    public override string AlertDescription { get; set; } =
        "🎯 Выстрелить в экран указанными тобой смайликами!";
    public override Color Color { get; set; } = Color.FromArgb(31, 255, 72);
    public override int Cost { get; init; } = 1702;
    public override Func<bool> IsRewardEnabled { get; set; } = () => true;

    private Task HandleRewardInputAsync(RewardInputChatMessageEvent e, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = e.Message;

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
                await _hubContext.Clients.All.MakeScreenEmojisParticles(
                    new ScreenEmojisParticlesEvent { Message = message.Message }
                );
            }
        );

        _logger.LogInformation("Emojis reward activated by {UserName}", message.UserName);
        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            _lifetime,
            _rewardEventManager,
            onRedeem: ValidatedRedeem(_validator, OnRewardRedeemedAsync, Cost, IsRewardEnabled),
            onRewardInput: HandleRewardInputAsync
        );
        return Task.CompletedTask;
    }

    protected override Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    ) => Task.CompletedTask;
}
