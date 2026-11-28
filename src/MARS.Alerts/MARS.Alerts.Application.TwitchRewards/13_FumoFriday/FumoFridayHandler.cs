using System.Collections.Concurrent;
using System.Drawing;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Policies;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Alerts.Application.TwitchRewards._13_FumoFriday;

/// <summary>
/// Обработчик reward-события <c>RewardFumoFriday</c>.
/// </summary>
[ExcludeFromCodeCoverage]
public class FumoFridayHandler(
    ILogger<FumoFridayHandler> logger,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 13;
    public override string AlertDisplayName { get; set; } = "🌙 Fumo Friday";
    public override string AlertDescription { get; set; } =
        "Присоединись к Fumo Friday и получи персональное сообщение от фумо каждую пятницу! ♪";
    public override Color Color { get; set; } = Color.FromArgb(255, 192, 203);
    public override int Cost { get; init; } = 13;

    public override IRewardActivationPolicy Activation => new DayOfWeekPolicy(DayOfWeek.Friday);

    public override Type? CommandPayloadType => typeof(FumoAlertCommandPayload);

    public override async Task<string> InvokeFromCommandAsync(
        AlertCommandPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        if (payload is not FumoAlertCommandPayload fumoPayload)
        {
            return "Неверный тип параметров для fumo.";
        }

        var displayName = fumoPayload.DisplayName.TrimStart('@');
        var twitchUser = new MARS.Shared.Kernel.TwitchUser
        {
            TwitchId = string.Empty,
            UserLogin = displayName,
            DisplayName = displayName,
            ChatColor = fumoPayload.Color,
        };

        await hubContext.Clients.All.FumoFriday(new FumoFridayEvent { User = twitchUser });
        return $"Фумо фрайдей с {displayName} объявлен!";
    }

    private readonly ConcurrentDictionary<string, DateTimeOffset> _shown = new();

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeOnStart(
            lifetime,
            rewardEventManager,
            onRedeem: ValidatedRedeem(validator, OnRewardRedeemedAsync, Cost, Activation)
        );
        return Task.CompletedTask;
    }

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

                await hubContext.Clients.All.FumoFriday(new FumoFridayEvent { User = user });
            }
        );

        logger.LogInformation("FumoFriday activated by {UserName}", rewardEvent.UserName);
    }
}
