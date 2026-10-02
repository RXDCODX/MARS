namespace MARS.TwitchCore.Services.TekkenStreams;

/// <summary>
/// Решения по чатам теккен-стримов: какие подключать, какие покидать и как
/// оформлять пересылку в Discord. Правила перенесены из
/// <c>TekkenStreamsDiscordForwarderService</c> монолита, где они были
/// размазаны по четырём методам и потому не проверялись.
/// </summary>
public static class TekkenStreamsSyncPolicy
{
    /// <summary>Категория Tekken в Twitch (Game ID).</summary>
    public const string TekkenGameId = "538054672";

    /// <summary>Язык стримов: только русскоязычные.</summary>
    public const string StreamLanguage = "ru";

    /// <summary>Интервал обновления списка стримов. В монолите — пять минут.</summary>
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    /// <summary>Пауза между подключениями: IRC не любит частых входов.</summary>
    public static readonly TimeSpan JoinDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Каналы, в которые нужно войти: текущие теккен-стримы, в которых нас
    /// ещё нет и которые не являются нашим каналом.
    /// </summary>
    public static IReadOnlyList<string> BuildToJoin(
        IEnumerable<string> streamLogins,
        IEnumerable<string> joinedChannels,
        string ownChannel
    )
    {
        var joined = new HashSet<string>(joinedChannels, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var login in streamLogins)
        {
            if (
                !string.IsNullOrWhiteSpace(login)
                && !login.Equals(ownChannel, StringComparison.OrdinalIgnoreCase)
                && !joined.Contains(login)
            )
            {
                result.Add(login);
            }
        }

        return result;
    }

    /// <summary>
    /// Каналы, из которых нужно выйти: все подключённые, кроме текущих
    /// стримов и собственного канала.
    /// </summary>
    public static IReadOnlyList<string> BuildToLeave(
        IEnumerable<string> joinedChannels,
        IEnumerable<string> streamLogins,
        string ownChannel
    )
    {
        var live = new HashSet<string>(streamLogins, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var channel in joinedChannels)
        {
            if (
                !string.IsNullOrWhiteSpace(channel)
                && !channel.Equals(ownChannel, StringComparison.OrdinalIgnoreCase)
                && !live.Contains(channel)
            )
            {
                result.Add(channel);
            }
        }

        return result;
    }

    /// <summary>
    /// Пересылать ли сообщение из этого чата.
    /// </summary>
    public static bool ShouldForward(
        string? channel,
        IEnumerable<string> tekkenChannels,
        ulong discordChannelId
    )
    {
        if (string.IsNullOrWhiteSpace(channel) || discordChannelId == 0)
        {
            return false;
        }

        return tekkenChannels.Contains(channel, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Строка для Discord: канал, автор и текст.</summary>
    public static string FormatMessage(string channel, string displayName, string message)
    {
        return $"[{channel}] {displayName}: {message}";
    }
}
