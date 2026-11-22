using System;
using MARS.Alerts.Application.Extensions;
using MARS.Alerts.Models.Overlay;
using MARS.Alerts.Presentation.Overlay.Hubs;
using MARS.Platform.Infrastructure.Http;
using MARS.Shared.Contracts;
using MARS.Shared.Contracts.Alerts;
using MARS.Shared.Contracts.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace MARS.Alerts.Application.TwitchRewards.Services;

public class RickRollerService(
    IConfiguration configuration,
    IHubContext<AlertsHub, IAlertsHub> overlaySink
)
{
    private readonly Random _rnd = new();

    public double RickRollChance
    {
        get
        {
            var confValueRaw = configuration["AppSettings:RickRoll:Chance"];
            var confValue = confValueRaw?.Trim();
            if (!string.IsNullOrWhiteSpace(confValue))
            {
                if (
                    double.TryParse(
                        confValue,
                        System.Globalization.NumberStyles.Float
                            | System.Globalization.NumberStyles.AllowThousands,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var value
                    )
                )
                {
                    return value;
                }
                var replaced = confValue.Replace(',', '.');
                if (
                    double.TryParse(
                        replaced,
                        System.Globalization.NumberStyles.Float
                            | System.Globalization.NumberStyles.AllowThousands,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value
                    )
                )
                {
                    return value;
                }
            }
            return 0.03;
        }
    }

    public async Task<bool> TryRickRollAsync(TwitchUser user, Func<Task> whenNotRickRolled)
    {
        var roll = _rnd.NextDouble();
        if (roll < RickRollChance)
        {
            MediaInfo newDto = new()
            {
                FileInfo = new MediaFileInfo()
                {
                    Extension = ".mp4",
                    FileName = "rickroll.mp4",
                    FilePath = "Alerts/rickroll.mp4",
                    Type = MediaType.Video,
                    IsLocalFile = true,
                },
                MetaInfo = new MediaMetaInfo()
                {
                    DisplayName = string.Empty,
                    Duration = 7,
                    IsLooped = false,
                    Priority = MediaAlertPriority.Normal,
                },
                PositionInfo = new MediaPositionInfo()
                {
                    IsRotated = true,
                    IsUseOriginalWidthAndHeight = true,
                    RandomCoordinates = true,
                },
                StylesInfo = new MediaStylesInfo(),
                TextInfo = new MediaTextInfo()
                {
                    Text = "#{user.name}# был рикрольнут на баллы канала!",
                    KeyWordSybmolDelimiter = '#',
                },
                Id = Guid.NewGuid(),
            };

            newDto.FixAlertText(user.DisplayName, string.Empty);
            newDto.TextInfo.KeyWordsColor = user.ChatColor;

            await overlaySink.Clients.All.Alert(new AlertEvent { Media = newDto });
            return true;
        }
        else
        {
            await whenNotRickRolled();
            return false;
        }
    }
}
