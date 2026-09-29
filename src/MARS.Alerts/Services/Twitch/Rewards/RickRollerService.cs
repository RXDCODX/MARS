using MARS.Alerts.Extensions;
using MARS.Shared.Hubs;
using MARS.Shared.Hubs.Interfaces;
using MARS.Shared.Hubs.Models;
using MARS.Alerts.Models;
using MARS.Shared.Models;
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.SignalR;

namespace MARS.Alerts.Services.Twitch.Rewards;

public class RickRollerService(
    IHubContext<TelegramusHub, ITelegramusHub> hubContext,
    IConfiguration configuration
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
                    FilePath = "Alerts\\rickroll.mp4",
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

            await hubContext.Clients.All.Alert(new MediaDto(newDto));
            return true;
        }
        else
        {
            await whenNotRickRolled();
            return false;
        }
    }
}
