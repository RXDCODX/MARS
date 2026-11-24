using System.Drawing;
using System.Text.Json;
using MARS.Alerts.Application.TwitchRewards.Handlers;
using MARS.Alerts.Application.TwitchRewards.Models;
using MARS.Alerts.Application.TwitchRewards.Routing;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Application.TwitchRewards.Services.Validation;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Messaging;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Twitch.Rest;
using MARS.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MARS.Alerts.Application.TwitchRewards._18_GaoAlert;

/// <summary>
/// Обработчик reward-события <c>RewardGaoAlert</c>.
/// </summary>
public class GaoAlertHandler(
    ILogger<GaoAlertHandler> logger,
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext,
    ITwitchUsersClient twitchUsersClient,
    ITwitchEventValidationService validator,
    IHostApplicationLifetime lifetime,
    IRewardEventManager rewardEventManager
) : RewardHandlerBase
{
    public override int RewardCost { get; } = 18;
    public override string AlertDisplayName { get; set; } = "🦁 GAO Alert";
    public override string AlertDescription { get; set; } = "";
    public override Color Color { get; set; } = Color.FromArgb(255, 255, 0);
    public override int Cost { get; init; } = 18;

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
        var text = (rewardEvent.UserInput ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("GaoAlert: empty input from {UserName}", rewardEvent.UserName);
            return;
        }

        var isJustText = text.Contains(' ');

        if (isJustText)
        {
            var dto = new GaoAlertDto { IsJustText = true, JustText = text };
            await PublishAsync(dto, rewardEvent);
            return;
        }

        var normalized = text.TrimStart('@');

        var isValidTwitchUsername =
            !string.IsNullOrWhiteSpace(normalized)
            && normalized.Length <= 25
            && System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[a-zA-Z0-9_]+$");

        if (isValidTwitchUsername)
        {
            try
            {
                var restUser = await twitchUsersClient.GetFullUserByLoginAsync(normalized, ct);
                if (restUser is not null)
                {
                    var twitchUser = new TwitchUser
                    {
                        TwitchId = restUser.TwitchId,
                        UserLogin = restUser.UserLogin,
                        DisplayName = restUser.DisplayName,
                        ProfileImageUrl = restUser.ProfileImageUrl,
                        ChatColor = restUser.ChatColor,
                        IsModerator = restUser.IsModerator,
                        IsVip = restUser.IsVip,
                    };
                    var dto = new GaoAlertDto { TwitchUser = twitchUser, IsJustText = false };
                    await PublishAsync(dto, rewardEvent);
                    logger.LogInformation("Gao alert with user {userName}", twitchUser.DisplayName);
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GaoAlert: failed to lookup user {UserName}", normalized);
            }
        }

        var fallback = new GaoAlertDto { IsJustText = true, JustText = text };
        await PublishAsync(dto: fallback, rewardEvent);
        logger.LogInformation("Gao alert with user {text}", text);
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
                await hubContext.Clients.All.GaoAlert(
                    new GaoAlertEvent
                    {
                        Id = dto.Id,
                        TwitchUser = dto.TwitchUser as TwitchUser,
                        IsJustText = dto.IsJustText,
                        JustText = dto.JustText,
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
