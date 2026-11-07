using MARS.Modules.Alerts.Twitch.Rewards.Application.Extensions;
using MARS.Shared.Contracts.Grpc.Models;
using MARS.Shared.Contracts.Models.Media;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MARS.Modules.Alerts.Twitch.Rewards.Application.Services.PyroAlerts;

public class TgFileInfo
{
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
}
