using Microsoft.Extensions.Logging;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace MARS.TwitchCore.Extensions;

public static class TwitchClientExtensions
{
    private const int MaxMessageLength = 500;

    private static List<string> SplitMessage(string message)
    {
        var normalized = message.Replace("\r\n", " ").Replace("\n", " ").Replace("  ", " ").Trim();
        return SplitBySpaces(normalized);
    }

    private static List<string> SplitBySpaces(string text)
    {
        var chunks = new List<string>();

        while (text.Length > MaxMessageLength)
        {
            var splitAt = text.LastIndexOf(' ', MaxMessageLength);
            if (splitAt <= 0)
            {
                splitAt = MaxMessageLength;
            }

            chunks.Add(text[..splitAt]);
            text = text[splitAt..].TrimStart(' ');
        }

        if (text.Length > 0)
        {
            chunks.Add(text);
        }

        return chunks;
    }

    extension(ITwitchClient client)
    {
        public async Task SendMessageToMainTwitchAsync<T>(string message, ILogger<T>? logger = null)
            where T : class
        {
            try
            {
                if (
                    !client.JoinedChannels.Any(e =>
                        e.Channel.Equals(
                            TwitchConstants.Channel,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                {
                    await client.JoinChannelAsync(TwitchConstants.Channel);
                }

                JoinedChannel? channel = client.GetJoinedChannel(TwitchConstants.Channel);
                if (channel != null)
                {
                    foreach (var chunk in SplitMessage(message))
                    {
                        await client.SendMessageAsync(channel, chunk);
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogException(e);
            }
        }

        public async Task SendMessageToMainTwitchAsync(string message, ILogger? logger = null)
        {
            try
            {
                if (
                    !client.JoinedChannels.Any(e =>
                        e.Channel.Equals(
                            TwitchConstants.Channel,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                {
                    await client.JoinChannelAsync(TwitchConstants.Channel);
                }

                JoinedChannel? channel = client.GetJoinedChannel(TwitchConstants.Channel);
                if (channel != null)
                {
                    foreach (var chunk in SplitMessage(message))
                    {
                        await client.SendMessageAsync(channel, chunk);
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogException(e);
            }
        }
    }
}
