using System.Text.Json;
using MARS.Alerts.Application.Twitch.Rewards.Services;
using MARS.Platform.Infrastructure.Events;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Models;
using Microsoft.AspNetCore.Hosting;

namespace MARS.Alerts.Application.Twitch.Rewards;

public class HighlitedMessage(
    RickRollerService rickRollerService,
    EventBroadcaster<TelegramusEvent> broadcaster
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

                        await broadcaster.BroadcastAsync(
                            new TelegramusEvent
                            {
                                Highlite = new HighliteEvent
                                {
                                    MessageJson = JsonSerializer.Serialize(messageText),
                                    Color = color,
                                    FaceUrlJson = "",
                                },
                            }
                        );
                    }
                );
            });
        }
    }
}
