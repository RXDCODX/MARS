using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services.SoundCloud;
using MARS.SoundRequest.Services.Spotify;
using MARS.SoundRequest.Services.YouTube;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.SoundRequest.Services;

public class SoundRequestCommandsService(
    YouTubeResolver ytResolver,
    SpotifyResolver spotifyResolver,
    SoundCloudResolver soundCloudResolver,
    SoundRequestUserQueue queue,
    IDbContextFactory<MediaDbContext> dbFactory,
    MainPlayer mainPlayer,
    StateManager stateManager,
    InSignalRHubService inSignalRHubService,
    IOptions<SoundRequestConfiguration> soundRequestOptions
)
{
    public async Task<string> AddTrackAsync(
        string query,
        string requestedByTwitchId,
        CancellationToken cancellationToken = default
    )
    {
        var result = string.Empty;

        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(requestedByTwitchId))
        {
            result = "Неверные параметры запроса";
            return result;
        }

        var playerState = await stateManager.GetStateAsync();

        if (playerState.State == PlaybackState.Stopped)
        {
            result = "Прием реквестов приостановлен - плеер остановлен";
            return result;
        }

        var normalizedQuery = NormalizeUrl(query);
        var isSoundCloudUrl = IsSoundCloudUrl(normalizedQuery);
        var isSpotifyUrl = IsSpotifyUrl(normalizedQuery);
        var isYouTubeAllowed = IsPlatformAllowed("YouTube");
        var isSpotifyAllowed = IsPlatformAllowed("Spotify");
        var isSoundCloudAllowed = IsPlatformAllowed("SoundCloud");

        if (!isYouTubeAllowed && !isSpotifyAllowed && !isSoundCloudAllowed)
        {
            result = "SoundRequest отключен в конфигурации";
            return result;
        }

        // Проверяем, является ли запрос URL
        BaseTrackInfo? info = null;
        if (Uri.TryCreate(normalizedQuery, UriKind.Absolute, out _))
        {
            var sourceTrackId = ExtractSourceTrackId(normalizedQuery);

            if (!string.IsNullOrWhiteSpace(sourceTrackId))
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
                info = await db
                    .Tracks.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.VideoId == sourceTrackId, cancellationToken);
            }

            if (info == null && isSoundCloudUrl)
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
                if (Uri.TryCreate(normalizedQuery, UriKind.Absolute, out var trackUri))
                {
                    info = await db
                        .Tracks.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Url == trackUri, cancellationToken);
                }
            }

            if (info == null)
            {
                if (isSoundCloudUrl && isSoundCloudAllowed)
                {
                    info = await soundCloudResolver.ResolveTrackAsync(normalizedQuery, cancellationToken);
                }
                else if (isSpotifyUrl && isSpotifyAllowed)
                {
                    info = await spotifyResolver.ResolveTrackAsync(normalizedQuery, cancellationToken);
                }
                else if (isYouTubeAllowed)
                {
                    info = await ytResolver.ResolveVideoAsync(normalizedQuery, cancellationToken);
                }
            }
        }
        else
        {
            // Поисковый запрос
            if (isSpotifyAllowed)
            {
                info = await spotifyResolver.ResolveQueryAsync(query, cancellationToken);
            }
            else if (isYouTubeAllowed)
            {
                info = await ytResolver.ResolveQueryAsync(query, cancellationToken);
            }
        }

        if (info != null)
        {
            var maxDuration = TimeSpan.FromMinutes(12);
            if (info.Duration > maxDuration)
            {
                var durationMinutes = Math.Round(info.Duration.TotalMinutes, 1);
                result = $"❌ Трек слишком длинный ({durationMinutes} мин). Максимальная длительность: 12 минут";
                return result;
            }

            var currentState = await stateManager.GetStateAsync();
            var wasPlayerStopped = currentState.State == PlaybackState.Stopped;
            var wasPlayerWaiting = currentState.State == PlaybackState.WaitingForTrack;
            var queueCountBefore = await queue.GetQueueCountAsync();

            var requestedAt = DateTime.Now;
            var queueItem = await queue.AddToQueueAsync(info, requestedByTwitchId, requestedAt);

            if (queueItem != null)
            {
                if ((wasPlayerStopped || wasPlayerWaiting) && queueCountBefore == 0)
                {
                    await mainPlayer.PlayAsync(queueItem, cancellationToken);
                    await NotifyQueueChangedAsync();
                }
                else
                {
                    await NotifyQueueChangedAsync();
                }

                var duration = info.Duration;
                var durationText =
                    duration > TimeSpan.Zero
                        ? $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}"
                        : "??:??";

                var waitTime = await CalculateWaitTimeAsync(queueItem.QueueOrder);
                var waitTimeText = FormatWaitTime(waitTime);

                result = $"Добавлено: {info.Title} [{durationText}]{waitTimeText}";
            }
            else
            {
                result = "Не удалось добавить трек в очередь";
            }
        }
        else
        {
            if (isSoundCloudUrl)
            {
                result = isSoundCloudAllowed
                    ? "не удалось распознать трек SoundCloud по ссылке"
                    : "SoundCloud отключен в конфигурации SoundRequest";
            }
            else if (isSpotifyUrl)
            {
                result = isSpotifyAllowed
                    ? "не удалось распознать трек Spotify по запросу"
                    : "Spotify отключен в конфигурации SoundRequest";
            }
            else
            {
                result = isYouTubeAllowed
                    ? "не удалось распознать видео по ссылке"
                    : "YouTube отключен в конфигурации SoundRequest";
            }
        }

        return result;
    }

    public async Task<string> AddTrackAsync(
        BaseTrackInfo track,
        string requestedByTwitchId,
        CancellationToken cancellationToken = default
    )
    {
        var result = string.Empty;

        if (track == null || string.IsNullOrWhiteSpace(requestedByTwitchId))
        {
            result = "Неверные параметры запроса";
            return result;
        }

        var playerState = await stateManager.GetStateAsync();

        if (playerState.State == PlaybackState.Stopped)
        {
            result = "Прием реквестов приостановлен - плеер остановлен";
            return result;
        }

        var maxDuration = TimeSpan.FromMinutes(12);
        if (track.Duration > maxDuration)
        {
            var durationMinutes = Math.Round(track.Duration.TotalMinutes, 1);
            result = $"❌ Трек слишком длинный ({durationMinutes} мин). Максимальная длительность: 12 минут";
            return result;
        }

        var currentState = await stateManager.GetStateAsync();
        var wasPlayerStopped = currentState.State == PlaybackState.Stopped;
        var wasPlayerWaiting = currentState.State == PlaybackState.WaitingForTrack;
        var queueCountBefore = await queue.GetQueueCountAsync();

        var requestedAt = DateTime.Now;
        var queueItem = await queue.AddToQueueAsync(track, requestedByTwitchId, requestedAt);

        if (queueItem != null)
        {
            if ((wasPlayerStopped || wasPlayerWaiting) && queueCountBefore == 0)
            {
                await mainPlayer.PlayAsync(queueItem, cancellationToken);
                await NotifyQueueChangedAsync();
            }
            else
            {
                await NotifyQueueChangedAsync();
            }

            var duration = track.Duration;
            var durationText =
                duration > TimeSpan.Zero
                    ? $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}"
                    : "??:??";

            result = $"Добавлено: {track.Title} [{durationText}]";
        }
        else
        {
            result = "Не удалось добавить трек в очередь";
        }

        return result;
    }

    public async Task<string> AddPlaylistAsync(
        string playlistUrl,
        string requestedByTwitchId,
        int maxTracksToAdd = 10,
        CancellationToken cancellationToken = default
    )
    {
        string result;

        var playerState = await stateManager.GetStateAsync();

        if (playerState.State == PlaybackState.Stopped)
        {
            result = "Прием реквестов приостановлен - плеер остановлен";
            return result;
        }

        BaseTrackInfo[]? items = null;

        if (IsSoundCloudUrl(playlistUrl))
        {
            items = await soundCloudResolver.ResolvePlaylistAsync(playlistUrl, cancellationToken);
        }
        else
        {
            items = await ytResolver.ResolvePlaylistAsync(playlistUrl);
        }

        if (items is { Length: > 0 })
        {
            var currentState = await stateManager.GetStateAsync();
            var wasPlayerStopped = currentState.State == PlaybackState.Stopped;
            var wasPlayerWaiting = currentState.State == PlaybackState.WaitingForTrack;
            var queueCountBefore = await queue.GetQueueCountAsync();

            QueueItem? firstQueueItem = null;
            var maxDuration = TimeSpan.FromMinutes(12);
            var skippedTracksCount = 0;
            var addedTracks = 0;
            var effectiveMaxTracksToAdd = maxTracksToAdd > 0 ? maxTracksToAdd : int.MaxValue;
            var requestedAt = DateTime.Now;

            foreach (var info in items)
            {
                if (addedTracks >= effectiveMaxTracksToAdd)
                {
                    break;
                }

                if (info.Duration > maxDuration)
                {
                    skippedTracksCount++;
                    continue;
                }

                var queueItem = await queue.AddToQueueAsync(info, requestedByTwitchId, requestedAt);
                firstQueueItem ??= queueItem;
                addedTracks++;
            }

            if ((wasPlayerStopped || wasPlayerWaiting) && queueCountBefore == 0 && firstQueueItem != null)
            {
                await mainPlayer.PlayAsync(firstQueueItem, cancellationToken);
                await NotifyQueueChangedAsync();
            }
            else
            {
                await NotifyQueueChangedAsync();
            }

            var addedCount = addedTracks;
            result = $"Добавлено треков: {addedCount}";

            if (skippedTracksCount > 0)
            {
                result += $" (пропущено {skippedTracksCount} треков)";
            }

            if (maxTracksToAdd > 0 && items.Count(i => i.Duration <= maxDuration) > maxTracksToAdd)
            {
                result += $" (ограничено до {maxTracksToAdd} треков)";
            }
        }
        else
        {
            result = "Не удалось прочитать плейлист";
        }

        return result;
    }

    public async Task<string> GetCurrentSongAsync(CancellationToken cancellationToken = default)
    {
        string result;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var last = await db
            .Tracks.AsNoTracking()
            .OrderByDescending(t => t.LastTimePlays)
            .FirstOrDefaultAsync(cancellationToken);

        if (last != null)
        {
            result = $"Сейчас: {last.Title}";
        }
        else
        {
            result = "Нет информации о текущей песне";
        }

        return result;
    }

    public async Task<string> GetUserQueuePositionAsync(string twitchId)
    {
        var result = string.Empty;

        if (!string.IsNullOrWhiteSpace(twitchId))
        {
            var list = await queue.GetQueueAsync();
            var idx = list.FindIndex(qi => qi.RequestedByTwitchId == twitchId);

            result = idx >= 0 ? $"Ваша позиция в очереди: {idx + 1}" : "Вы не в очереди";
        }
        else
        {
            result = "Не удалось определить пользователя";
        }

        return result;
    }

    public async Task<string> CancelLastTrackAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        string result;

        if (string.IsNullOrWhiteSpace(twitchId))
        {
            result = "Не удалось определить пользователя";
        }
        else
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var userQueueItemsQuery = db
                .QueueItems.Include(qi => qi.Track)
                .Where(qi => qi.RequestedByTwitchId == twitchId && qi.QueueOrder >= 0)
                .AsQueryable();

            if (await userQueueItemsQuery.AnyAsync(cancellationToken))
            {
                var lastRequestedAt = await userQueueItemsQuery.MaxAsync(
                    qi => qi.RequestedAt,
                    cancellationToken
                );

                var itemsToCancel = await userQueueItemsQuery
                    .Where(qi => qi.RequestedAt == lastRequestedAt)
                    .OrderByDescending(qi => qi.QueueOrder)
                    .ToListAsync(cancellationToken);

                if (itemsToCancel is not { Count: 0 })
                {
                    foreach (var queueItem in itemsToCancel)
                    {
                        await queue.RemoveFromQueueAsync(queueItem.Id);
                    }

                    await NotifyQueueChangedAsync();

                    if (itemsToCancel is [{ Track: not null }])
                    {
                        result = $"Отменён трек: {itemsToCancel[0].Track?.Title ?? "Пусто"}";
                    }
                    else
                    {
                        result = $"Отменён плейлист из {itemsToCancel.Count} треков";
                    }
                }
                else
                {
                    result = "Нечего отменять";
                }
            }
            else
            {
                result = "Нечего отменять";
            }
        }

        return result;
    }

    public async Task<string> ClearQueueAsync(CancellationToken cancellationToken = default)
    {
        var result = "Очередь уже пуста";

        try
        {
            var queueCount = await queue.GetQueueCountAsync();

            if (queueCount > 0)
            {
                await stateManager.StopPlaybackAsync(notify: true);

                var removedCount = await queue.ClearQueueAsync();
                await NotifyQueueChangedAsync();

                result =
                    removedCount > 0
                        ? $"Очередь очищена, удалено треков: {removedCount}"
                        : "Очередь уже пуста";
            }
        }
        catch (Exception ex)
        {
            result = $"❌ Исключение: {ex.Message}";
        }

        return result;
    }

    public async Task<string> PlayQueueItemNowAsync(
        Guid queueItemId,
        CancellationToken cancellationToken = default
    )
    {
        var result = "❌ Ошибка при выполнении";

        if (queueItemId == Guid.Empty)
        {
            result = "❌ ID трека не может быть пустым";
        }
        else
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
                var queueItem = await db
                    .QueueItems.AsNoTracking()
                    .Include(queueItem => queueItem.Track)
                    .FirstOrDefaultAsync(qi => qi.Id == queueItemId, cancellationToken);

                if (queueItem is null)
                {
                    result = "❌ Трек не найден в очереди";
                }
                else if (queueItem.QueueOrder == 0)
                {
                    result = "❌ Этот трек уже сейчас играет";
                }
                else
                {
                    var track = queueItem.Track;

                    if (track is null)
                    {
                        track = await db
                            .Tracks.AsNoTracking()
                            .FirstOrDefaultAsync(
                                trackItem => trackItem.Id == queueItem.TrackId,
                                cancellationToken
                            );
                    }

                    if (track is null)
                    {
                        result = "❌ Информация о треке недоступна";
                    }
                    else if (result == "❌ Ошибка при выполнении")
                    {
                        queueItem.Track = track;

                        var movedItem = await queue.MoveToFrontAndPlayAsync(queueItemId);

                        if (movedItem?.Track is not null)
                        {
                            await mainPlayer.PlayAsync(movedItem, cancellationToken);
                            await NotifyQueueChangedAsync();

                            result = $"▶️ Сейчас играет: {movedItem.Track!.Title}";
                        }
                        else
                        {
                            result = "❌ Не удалось запустить трек";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = $"❌ Исключение: {ex.Message}";
            }
        }

        return result;
    }

    public async Task<string> ReorderQueueItemAsync(
        Guid queueItemId,
        int newPosition,
        CancellationToken cancellationToken = default
    )
    {
        string result;

        if (queueItemId == Guid.Empty)
        {
            return "❌ ID элемента не может быть пустым";
        }

        try
        {
            var moved = await queue.MoveQueueItemToPositionAsync(queueItemId, newPosition);
            if (moved is null)
            {
                result = "❌ Элемент не найден или позиция некорректна";
            }
            else
            {
                await NotifyQueueChangedAsync();
                result = "✅ Позиция обновлена";
            }
        }
        catch (Exception ex)
        {
            result = $"❌ Исключение: {ex.Message}";
        }

        return result;
    }

    public async Task<string> StopPlaybackAsync(CancellationToken cancellationToken = default)
    {
        string result;

        try
        {
            await stateManager.StopPlaybackAsync(notify: true);
            await NotifyQueueChangedAsync();
            result = "⏹ Воспроизведение остановлено";
        }
        catch (Exception ex)
        {
            result = $"❌ Исключение: {ex.Message}";
        }

        return result;
    }

    public async Task<string> ResumePlaybackAsync(CancellationToken cancellationToken = default)
    {
        string result;

        try
        {
            var currentState = await stateManager.GetStateAsync();

            if (currentState.State == PlaybackState.Paused)
            {
                await stateManager.SetPausedAsync(false, notify: true);
                result = "▶️ Воспроизведение возобновлено";
            }
            else if (currentState.State == PlaybackState.Stopped)
            {
                var queueList = await queue.GetQueueAsync();
                var first = queueList.FirstOrDefault(qi => qi.QueueOrder > 0);
                if (first != null)
                {
                    await mainPlayer.PlayAsync(first, cancellationToken);
                    await NotifyQueueChangedAsync();
                    result =
                        first.Track != null
                            ? $"▶️ Сейчас играет: {first.Track.Title}"
                            : "▶️ Воспроизведение запущено";
                }
                else
                {
                    result = "Нет треков в очереди";
                }
            }
            else
            {
                result = "Уже воспроизводится";
            }
        }
        catch (Exception ex)
        {
            result = $"❌ Исключение: {ex.Message}";
        }

        return result;
    }

    public async Task<string> PausePlaybackAsync(CancellationToken cancellationToken = default)
    {
        string result;

        try
        {
            var currentState = await stateManager.GetStateAsync();

            if (currentState.State != PlaybackState.Stopped)
            {
                await stateManager.SetPausedAsync(true, notify: true);
                result = "⏸️ Воспроизведение остановлено";
            }
            else
            {
                result = "Нельзя поставить паузу если воспроизведение остановлено";
            }
        }
        catch (Exception ex)
        {
            result = $"❌ Исключение: {ex.Message}";
        }

        return result;
    }

    private async Task NotifyQueueChangedAsync()
    {
        var currentQueue = await queue.GetQueueAsync();
        await inSignalRHubService.NotifyQueueChangedAsync(currentQueue);
    }

    private async Task<TimeSpan> CalculateWaitTimeAsync(int queueOrder)
    {
        var result = TimeSpan.Zero;

        if (queueOrder > 0)
        {
            var currentState = await stateManager.GetStateAsync();

            if (currentState is { State: PlaybackState.Playing, CurrentQueueItem.Track: not null })
            {
                var currentTrack = currentState.CurrentQueueItem.Track;
                var progress = currentState.CurrentTrackProgress.GetValueOrDefault();
                var remainingTicks = currentTrack.Duration.Ticks - progress.Ticks;

                if (remainingTicks > 0)
                {
                    result += TimeSpan.FromTicks(remainingTicks);
                }
            }

            var queueList = await queue.GetQueueAsync();
            var tracksBeforeCurrent = queueList.Where(qi =>
                qi.QueueOrder < queueOrder && qi.Track != null
            );

            foreach (var queueItem in tracksBeforeCurrent)
            {
                var track = queueItem.Track;

                if (track is not null && track.Duration > TimeSpan.Zero)
                {
                    result += track.Duration;
                }
            }
        }

        return result;
    }

    private static string FormatWaitTime(TimeSpan waitTime)
    {
        var result = string.Empty;

        if (waitTime > TimeSpan.Zero)
        {
            var totalMinutes = (int)waitTime.TotalMinutes;
            var seconds = waitTime.Seconds;

            if (totalMinutes < 1)
            {
                result = seconds > 0 ? $" через ~ {seconds} сек" : " (меньше секунды)";
            }
            else if (totalMinutes == 1)
            {
                result = seconds > 0 ? $" через ~ 1 мин {seconds} сек" : " через ~ минута";
            }
            else if (totalMinutes < 60)
            {
                result =
                    seconds > 0
                        ? $" через ~ {totalMinutes} мин {seconds} сек"
                        : $" через ~ {totalMinutes} мин";
            }
            else
            {
                var hours = totalMinutes / 60;
                var minutes = totalMinutes % 60;

                if (minutes > 0 && seconds > 0)
                {
                    result = $" через ~ {hours} ч {minutes} мин {seconds} сек";
                }
                else if (minutes > 0)
                {
                    result = $" через ~ {hours} ч {minutes} мин";
                }
                else if (seconds > 0)
                {
                    result = $" через ~ {hours} ч {seconds} сек";
                }
                else
                {
                    result = $" через ~ {hours} ч";
                }
            }
        }

        return result;
    }

    private static string NormalizeUrl(string url)
    {
        var result = url;

        if (!string.IsNullOrWhiteSpace(url))
        {
            var trimmedUrl = url.Trim();

            var hasScheme =
                trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            if (
                !hasScheme
                && trimmedUrl.Contains('.')
                && !trimmedUrl.Contains(' ')
                && (
                    trimmedUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                    || trimmedUrl.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                    || trimmedUrl.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
                    || trimmedUrl.Contains("spotify.com", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                result = $"https://{trimmedUrl}";
            }
            else
            {
                result = trimmedUrl;
            }
        }

        return result;
    }

    private static bool IsSpotifyUrl(string url)
    {
        var result = false;

        if (!string.IsNullOrWhiteSpace(url))
        {
            result =
                url.Contains("spotify.com", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("spotify:track:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("spotify:album:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("spotify:playlist:", StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static bool IsSoundCloudUrl(string url)
    {
        var result = false;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            result =
                uri.Host.Contains("soundcloud.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Contains("snd.sc", StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private string? ExtractSourceTrackId(string normalizedQuery)
    {
        string? result = null;

        if (IsSoundCloudUrl(normalizedQuery) && IsPlatformAllowed("SoundCloud"))
        {
            result = $"soundcloud:{normalizedQuery}";
        }
        else if (IsSpotifyUrl(normalizedQuery) && IsPlatformAllowed("Spotify"))
        {
            var spotifyTrackId = spotifyResolver.ExtractTrackId(normalizedQuery);
            if (!string.IsNullOrWhiteSpace(spotifyTrackId))
            {
                result = $"spotify:{spotifyTrackId}";
            }
        }
        else
        {
            result = YouTubeResolver.ExtractVideoId(normalizedQuery);
        }

        return result;
    }

    private static string? ExtractYouTubeVideoId(string url)
    {
        return YouTubeResolver.ExtractVideoId(url);
    }

    private bool IsPlatformAllowed(string platformName)
    {
        var result = false;
        var enabledPlatforms = soundRequestOptions.Value.EnabledPlatforms;

        if (enabledPlatforms.Length > 0)
        {
            foreach (var enabledPlatform in enabledPlatforms)
            {
                if (enabledPlatform.Trim().Equals(platformName, StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    break;
                }
            }
        }

        return result;
    }
}
