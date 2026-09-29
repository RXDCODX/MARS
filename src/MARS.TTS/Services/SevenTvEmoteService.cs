using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SevenTV;

namespace MARS.TTS.Services;

public sealed class SevenTvEmoteService(
    ILogger<SevenTvEmoteService> logger,
    SevenTVClient? sevenTvClient = null
) : BackgroundService, ISevenTvEmoteService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

    private const string SevenTvUserId = "01G9FVE50G00022RD2T09E7QXC";

    internal readonly SevenTVClient _sevenTvClient = sevenTvClient ?? new();
    private readonly Lock _emotesLock = new();

    private HashSet<string> _emoteNames = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEmote(string word)
    {
        lock (_emotesLock)
        {
            return _emoteNames.Contains(word);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RefreshEmotesAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("7TV emote service cancelled during initial load");
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load 7TV emotes on startup");
        }

        using var timer = new PeriodicTimer(RefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshEmotesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
    }

    private async Task RefreshEmotesAsync(CancellationToken ct)
    {
        try
        {
            var fetchedEmotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sevenTvUser = await _sevenTvClient.rest.GetUser(SevenTvUserId);

            if (sevenTvUser is { emote_sets: { Length: > 0 } })
            {
                foreach (var emoteSet in sevenTvUser.emote_sets)
                {
                    if (emoteSet is { id: not null })
                    {
                        var emoteSetEmojis = await _sevenTvClient.rest.GetEmoteSet(emoteSet.id);

                        if (emoteSetEmojis is { emotes: { Length: > 0 } })
                        {
                            foreach (var emote in emoteSetEmojis.emotes)
                            {
                                if (emote is { name: not null })
                                {
                                    fetchedEmotes.Add(emote.name);
                                }
                            }
                        }
                    }
                }
            }

            if (fetchedEmotes.Count > 0)
            {
                lock (_emotesLock)
                {
                    _emoteNames = fetchedEmotes;
                }

                logger.LogInformation(
                    "Refreshed 7TV emotes: {Count} emotes loaded from API",
                    fetchedEmotes.Count
                );
            }
            else
            {
                logger.LogWarning(
                    "7TV emote list is empty for user {SevenTvUserId}. TTS emote filtering is disabled.",
                    SevenTvUserId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh 7TV emotes. Will retry on next timer tick.");
        }
    }
}
