using System.Collections.Concurrent;

namespace MARS.TwitchCore.Extensions;

public static class TwitchConstants
{
    public const string ChannelId = "785975641";
    public const string Channel = "rxdcodx";
    public const string BotName = "catisaai";
    public const string BotId = "888848441";
    public const string SevenTvUserId = "01G9FVE50G00022RD2T09E7QXC";

    /// <summary>
    /// Blacklisted user IDs. Updated by TwitchBlackListService.
    /// </summary>
    public static ConcurrentBag<string> BlackListedUserIds { get; set; } = [];
}
