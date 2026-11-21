using System.Text.Json;
using MARS.Alerts.Application.Services;
using MARS.Alerts.Application.TwitchRewards.Services;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Alerts.Presentation.Overlay.Hubs.Interfaces;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Application.TwitchRewards;

public class HighlitedMessage(
    RickRollerService rickRollerService,
    IHubContext<AlertsHub, IAlertsHub> hubContext
)
{
    public async Task HandleHighlightedMessage(
        TwitchUser user,
        string messageText,
        string hexColor,
        bool isVip,
        bool isModerator,
        bool isBroadcaster
    )
    {
        if ((isVip || isModerator || isBroadcaster))
        {
            await Task.Factory.StartNew(async () =>
            {
                await rickRollerService.TryRickRollAsync(
                    user,
                    async () =>
                    {
                        var color = string.IsNullOrWhiteSpace(hexColor) ? "#ffffff" : hexColor;

                        await hubContext.Clients.All.Highlite(
                            new HighliteEvent
                            {
                                MessageJson = JsonSerializer.Serialize(messageText),
                                Color = color,
                                FaceUrlJson = "",
                            }
                        );
                    }
                );
            });
        }
    }
}
