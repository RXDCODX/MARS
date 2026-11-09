using MARS.Alerts.Application.Twitch.Rewards.Extensions;
using MARS.Shared.Contracts.Grpc.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MARS.Alerts.Application.Twitch.Rewards.Services.PyroAlerts;

public class TgFileInfo
{
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
}
