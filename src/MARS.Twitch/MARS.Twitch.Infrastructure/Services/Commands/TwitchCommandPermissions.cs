using System.Diagnostics.CodeAnalysis;
using MARS.Twitch.Infrastructure.Extensions;

namespace MARS.Twitch.Infrastructure.Services.Commands;

/// <summary>
/// Нормализует права Twitch-вызывающего для MARS.Commands.
///
/// Глобальный admin-флаг соответствует монолиту: только стример. Модератор и VIP
/// передаются отдельными флагами, чтобы команды вроде title/srlist могли
/// применять свои command-specific правила, не расширяя глобальный доступ.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TwitchCommandPermissions
{
    public static bool IsBroadcaster(string? userId) =>
        !string.IsNullOrWhiteSpace(userId)
        && userId.Equals(TwitchConstants.ChannelId, StringComparison.OrdinalIgnoreCase);

    public static bool IsModerator(bool isModerator) => isModerator;

    public static bool IsVip(bool isVip) => isVip;

    public static bool IsAdmin(string? userId, bool isModerator) => IsBroadcaster(userId);
}
