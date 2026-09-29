using MARS.Shared.Extensions;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services.Interfaces;
using MARS.SoundRequest.Services.Spotify;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MARS.SoundRequest.Services;

public class MainPlayer(
    StateManager stateManager,
    InSignalRHubService inSignalRHubService,
    OutSignalRHubService outSignalRHubService,
    SoundRequestUserQueue queue,
    IDbContextFactory<MediaDbContext> dbFactory,
    IHostApplicationLifetime lifetime,
    SpotifyPlaybackService spotifyPlaybackService,
    IMarsSchemaReady<MediaDbContext> schemaReady,
    IOptions<SoundRequestConfiguration> soundRequestOptions,
    IOptions<SpotifySoundRequestConfiguration> spotifyOptions,
    ILogger<MainPlayer> logger
) : IPlayerController, IHostedService, IDisposable
{
    private const int MaxHistoryEntries = 1000;
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private readonly IMarsSchemaReady<MediaDbContext> _schemaReady = schemaReady;
    private readonly SoundRequestConfiguration _soundRequestConfiguration = soundRequestOptions.Value;
    private readonly SpotifySoundRequestConfiguration _spotifyConfiguration = spotifyOptions.Value;
    private readonly SemaphoreSlim _spotifyMonitorTransitionLock = new(1, 1);
    private Task? _spotifyMonitorTask;
    private Guid? _lastSpotifyCompletedQueueItemId;
    private DateTime _lastSpotifyTrackPlayIssuedAtUtc = DateTime.UnixEpoch;
    private bool _disposed;

    #region IHostedService

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        // Аудит (runtime-проверка compose): MainPlayer читал media.RootState раньше,
        // чем MarsSchemaMigrationHostedService применял миграции, и сервис падал
        // с 42P01. Дожидаемся готовности схемы своей БД.
        await _schemaReady.WaitAsync(cancellationToken);

        await stateManager.InitializeAsync();

        // Подписываемся на изменения состояния для отправки через SignalR
        stateManager.StateChanged += async (state, excludeConnectionId) =>
        {
            await inSignalRHubService.NotifyPlayerStateChangedAsync(state, excludeConnectionId);
        };

        // Wire OutSignalRHubService events for track lifecycle
        outSignalRHubService.OnEnded += OutSignalRHubServiceOnEnded;
        outSignalRHubService.OnStarted += OutSignalRHubServiceOnStarted;
        outSignalRHubService.OnError += OutSignalRHubServiceOnError;

        logger.LogInformation("MainPlayer started");

        if (await IsSpotifyModeAsync(_cancellationToken))
        {
            _spotifyMonitorTask = Task.Run(
                () => MonitorSpotifyPlaybackAsync(_cancellationToken),
                _cancellationToken
            );
        }
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        if (_spotifyMonitorTask != null)
        {
            try
            {
                await _spotifyMonitorTask;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Spotify playback monitor stopped with exception");
            }
        }

        logger.LogInformation("MainPlayer stopped");
    }

    #endregion

    #region SignalR Event Handlers

    private async Task OutSignalRHubServiceOnEnded(BaseTrackInfo arg)
    {
        logger.LogInformation(
            "[SignalR Event] Трек завершил воспроизведение: {TrackName} (ID: {TrackId})",
            arg.TrackName,
            arg.Id
        );

        await OnTrackEndedAsync(arg);
    }

    private async Task OutSignalRHubServiceOnStarted(BaseTrackInfo arg)
    {
        logger.LogInformation(
            "[SignalR Event] Трек начал воспроизведение: {TrackName} (ID: {TrackId})",
            arg.TrackName,
            arg.Id
        );

        await OnTrackStartedAsync(arg);
    }

    private async Task OutSignalRHubServiceOnError(BaseTrackInfo arg)
    {
        logger.LogError(
            "[SignalR Event] Получена ошибка воспроизведения трека: {TrackName} (ID: {TrackId})",
            arg.TrackName,
            arg.Id
        );

        await OnTrackErrorAsync(arg);
    }

    #endregion

    #region IPlayerController

    public async Task PlayAsync(QueueItem queueItem, CancellationToken _)
    {
        try
        {
            logger.LogInformation(
                "Начинаем воспроизведение трека: {TrackName}, URL: {Url}",
                queueItem.Track!.TrackName,
                queueItem.Track.Url
            );

            await stateManager.StartPlayingAsync(queueItem, notify: true);

            if (
                await IsSpotifyModeAsync(_cancellationToken)
                && spotifyPlaybackService.IsConfigured()
            )
            {
                var started = await spotifyPlaybackService.PlayTrackAsync(
                    queueItem.Track,
                    _cancellationToken
                );
                if (!started)
                {
                    throw new InvalidOperationException(
                        "Не удалось запустить трек в Spotify клиенте"
                    );
                }

                _lastSpotifyTrackPlayIssuedAtUtc = DateTime.Now;
            }

            await UpdateQueueItemLastPlayedAsync(queueItem);

            logger.LogInformation("Трек успешно запущен: {TrackName}", queueItem.Track.TrackName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка при воспроизведении трека: {TrackName}",
                queueItem.Track!.TrackName
            );
            await OutSignalRHubServiceOnError(queueItem.Track);
        }
    }

    public async Task PauseAsync(CancellationToken ct)
    {
        if (await IsSpotifyModeAsync(ct) && spotifyPlaybackService.IsConfigured())
        {
            await spotifyPlaybackService.PauseAsync(ct);
        }

        await stateManager.SetPausedAsync(true, notify: true);
    }

    public async Task ResumeAsync(CancellationToken ct)
    {
        if (await IsSpotifyModeAsync(ct) && spotifyPlaybackService.IsConfigured())
        {
            await spotifyPlaybackService.ResumeAsync(ct);
        }

        await stateManager.SetPausedAsync(false, notify: true);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (await IsSpotifyModeAsync(ct) && spotifyPlaybackService.IsConfigured())
        {
            await spotifyPlaybackService.StopAsync(ct);
        }

        await stateManager.StopPlaybackAsync(notify: true);
    }

    public async Task SkipAsync(CancellationToken ct)
    {
        var currentState = await stateManager.GetStateAsync();
        var currentTrack = currentState.CurrentQueueItem?.Track;

        if (currentTrack != null)
        {
            logger.LogInformation("Пропуск текущего трека: {TrackName}", currentTrack.TrackName);
        }

        await PlayNextFromQueueAsync();
    }

    public async Task SetVolumeAsync(float volume, CancellationToken ct)
    {
        if (await IsSpotifyModeAsync(ct) && spotifyPlaybackService.IsConfigured())
        {
            await spotifyPlaybackService.SetVolumeAsync((int)Math.Clamp(volume, 0f, 100f), ct);
        }

        await stateManager.SetVolumeAsync(volume, notify: true);
    }

    public async Task MuteAsync(CancellationToken ct)
    {
        if (await IsSpotifyModeAsync(ct) && spotifyPlaybackService.IsConfigured())
        {
            await spotifyPlaybackService.SetVolumeAsync(0, ct);
        }

        await stateManager.SetMutedAsync(true, notify: true);
    }

    public async Task UnmuteAsync(CancellationToken ct)
    {
        if (await IsSpotifyModeAsync(ct) && spotifyPlaybackService.IsConfigured())
        {
            var state = await stateManager.GetStateAsync();
            await spotifyPlaybackService.SetVolumeAsync(
                (int)Math.Clamp(state.Volume, 0f, 100f),
                ct
            );
        }

        await stateManager.SetMutedAsync(false, notify: true);
    }

    public async Task SetVideoDisplayAsync(VideoDisplay videoDisplay, CancellationToken ct)
    {
        await stateManager.SetVideoDisplayAsync(videoDisplay, notify: true);
    }

    public PlayerState GetState()
    {
        return stateManager.GetState();
    }

    #endregion

    #region Queue Management

    public async Task PlayNextFromQueueAsync()
    {
        var currentQueueItem = await queue.ShiftQueueAndGetCurrentAsync();

        if (currentQueueItem != null)
        {
            logger.LogInformation(
                "Начинаем воспроизведение трека: {TrackName}",
                currentQueueItem.Track!.TrackName
            );

            await CleanupOldHistoryAsync();
            await PlayAsync(currentQueueItem, _cancellationToken);
            await NotifyQueueChangedAsync();
        }
        else
        {
            logger.LogInformation("Очередь пуста - переводим плеер в состояние ожидания");
            await stateManager.SetCurrentQueueItemAsync(null, notify: true);
        }
    }

    public async Task PlayPreviousFromHistoryAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

        var previousQueueItem = await db
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .Where(qi => qi.QueueOrder == -1)
            .FirstOrDefaultAsync(_cancellationToken);

        if (previousQueueItem != null)
        {
            previousQueueItem.QueueOrder = 0;

            try
            {
                await db.QueueItems.ExecuteUpdateAsync(
                    e => e.SetProperty(t => t.QueueOrder, t => t.QueueOrder + 1),
                    cancellationToken: _cancellationToken
                );
            }
            catch (InvalidOperationException)
            {
                var queueItems = await db.QueueItems.ToListAsync(cancellationToken: _cancellationToken);
                foreach (var queueItem in queueItems)
                {
                    queueItem.QueueOrder += 1;
                }
                await db.SaveChangesAsync(_cancellationToken);
            }

            await PlayAsync(previousQueueItem, _cancellationToken);
        }
        else
        {
            logger.LogWarning("Не найден трек в истории для воспроизведения");
        }
    }

    public async Task EnsureCurrentQueueItemLoadedAsync()
    {
        var currentState = await stateManager.GetStateAsync();

        if (currentState.CurrentQueueItem == null)
        {
            var currentQueueItem = await queue.GetCurrentQueueItemAsync();

            if (currentQueueItem != null)
            {
                await stateManager.SetCurrentQueueItemAsync(currentQueueItem, notify: true);
            }
        }
    }

    public async Task PlayAsync()
    {
        var state = GetState();

        if (state.State == PlaybackState.Stopped || state.CurrentQueueItem == null)
        {
            await PlayNextFromQueueAsync();
        }
        else
        {
            await ResumeAsync(_cancellationToken);
        }
    }

    public async Task TogglePlayPauseAsync()
    {
        var state = GetState();

        if (state.State == PlaybackState.Paused)
        {
            await PlayAsync();
        }
        else
        {
            await PauseAsync(_cancellationToken);
        }
    }

    public async Task<List<QueueItem>> GetQueueAsync()
    {
        return await queue.GetQueueAsync();
    }

    public async Task PlayQueueItemAsync(Guid queueItemId)
    {
        var queueItem = await queue.GetQueueItemByIdAsync(queueItemId);

        if (queueItem != null)
        {
            if (queueItem.QueueOrder != 0)
            {
                var movedQueueItem = await queue.MoveToFrontAndPlayAsync(queueItemId);

                if (movedQueueItem != null)
                {
                    await PlayAsync(movedQueueItem, _cancellationToken);
                }
            }

            await NotifyQueueChangedAsync();
        }
    }

    public async Task RemoveQueueItemAsync(Guid queueItemId)
    {
        await queue.RemoveFromQueueAsync(queueItemId);
    }

    public async Task<List<BaseTrackInfo>> GetHistoryAsync(int count = 20)
    {
        await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

        var historyItems = await db
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .Where(qi => qi.QueueOrder < 0)
            .OrderByDescending(qi => qi.QueueOrder)
            .Take(count)
            .ToListAsync(_cancellationToken);

        return historyItems.Where(qi => qi.Track != null).Select(qi => qi.Track!).ToList();
    }

    public async Task<List<QueueItem>> GetHistoryQueueItemsAsync(int count = 20)
    {
        await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

        return await db
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .Where(qi => qi.QueueOrder < 0)
            .OrderByDescending(qi => qi.QueueOrder)
            .Take(count)
            .ToListAsync(_cancellationToken);
    }

    private async Task NotifyQueueChangedAsync()
    {
        var currentQueue = await queue.GetQueueAsync();
        await inSignalRHubService.NotifyQueueChangedAsync(currentQueue);
    }

    private async Task CleanupOldHistoryAsync()
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

            var historyQuery = db.QueueItems.Where(qi =>
                qi.QueueOrder < 0
                && !db.PlayerStates.Any(ps => ps.CurrentQueueItemId == qi.Id)
            );

            var historyCount = await historyQuery.CountAsync(_cancellationToken);
            var historyToDeleteCount = Math.Max(0, historyCount - MaxHistoryEntries);

            if (historyToDeleteCount > 0)
            {
                try
                {
                    await historyQuery
                        .OrderBy(qi => qi.QueueOrder)
                        .Take(historyToDeleteCount)
                        .ExecuteDeleteAsync(_cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    var oldHistoryItems = await historyQuery
                        .OrderBy(qi => qi.QueueOrder)
                        .Take(historyToDeleteCount)
                        .ToListAsync(_cancellationToken);

                    if (oldHistoryItems.Count > 0)
                    {
                        db.QueueItems.RemoveRange(oldHistoryItems);
                        await db.SaveChangesAsync(_cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при очистке истории");
        }
    }

    #endregion

    #region Track Events

    public async Task OnTrackEndedAsync(BaseTrackInfo track)
    {
        var currentState = await stateManager.GetStateAsync();
        var currentTrackId = currentState.CurrentQueueItem?.Track?.Id;
        var isEndedForCurrentTrack = currentTrackId.HasValue && currentTrackId.Value == track.Id;

        if (isEndedForCurrentTrack && currentState.State != PlaybackState.Stopped)
        {
            await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);
            var hasNextTracks = await db
                .QueueItems.AsNoTracking()
                .AnyAsync(qi => qi.QueueOrder > 0, _cancellationToken);

            if (!hasNextTracks)
            {
                await queue.ShiftQueueAndGetCurrentAsync();
                await CleanupOldHistoryAsync();
                await stateManager.SetCurrentQueueItemAsync(null, notify: true);
                await NotifyQueueChangedAsync();
            }
            else
            {
                await PlayNextFromQueueAsync();
            }
        }
    }

    public async Task OnTrackStartedAsync(BaseTrackInfo track)
    {
        await stateManager.NotifyStateChangedAsync();
    }

    public async Task OnTrackErrorAsync(BaseTrackInfo track)
    {
        logger.LogError("Ошибка воспроизведения трека: {TrackName} (ID: {TrackId})", track.TrackName, track.Id);

        await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);
        var hasNextTracks = await db
            .QueueItems.AsNoTracking()
            .AnyAsync(qi => qi.QueueOrder > 0, _cancellationToken);

        if (!hasNextTracks)
        {
            await stateManager.SetCurrentQueueItemAsync(null, notify: true);
        }
        else
        {
            await PlayNextFromQueueAsync();
        }
    }

    #endregion

    #region Spotify Monitoring

    private async Task MonitorSpotifyPlaybackAsync(CancellationToken ct)
    {
        var pollingInterval = Math.Max(750, _spotifyConfiguration.PollingIntervalMs);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (spotifyPlaybackService.IsConfigured())
                {
                    await HandleSpotifyPlaybackTickAsync(ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Ошибка Spotify playback monitor tick");
            }

            try
            {
                await Task.Delay(pollingInterval, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task HandleSpotifyPlaybackTickAsync(CancellationToken ct)
    {
        var state = await stateManager.GetStateAsync();
        var currentQueueItem = state.CurrentQueueItem;

        if (
            state.State == PlaybackState.Playing
            && currentQueueItem?.Track != null
            && spotifyPlaybackService.IsSpotifyTrack(currentQueueItem.Track)
        )
        {
            var playback = await spotifyPlaybackService.GetCurrentPlaybackAsync(ct);

            if (playback != null)
            {
                await stateManager.UpdateCurrentTrackProgressAsync(
                    TimeSpan.FromMilliseconds(Math.Max(0, playback.ProgressMs)),
                    notify: false
                );

                var expectedTrackId = spotifyPlaybackService.GetSpotifyTrackId(
                    currentQueueItem.Track
                );
                var isTrackChangedExternally =
                    !string.IsNullOrWhiteSpace(playback.TrackId)
                    && !string.IsNullOrWhiteSpace(expectedTrackId)
                    && !string.Equals(
                        playback.TrackId,
                        expectedTrackId,
                        StringComparison.OrdinalIgnoreCase
                    );

                var isTrackAlmostEnded =
                    playback is { IsPlaying: false, DurationMs: > 0 }
                    && playback.ProgressMs >= playback.DurationMs - 1200;

                var graceMs = Math.Max(0, _spotifyConfiguration.UserPlaybackPriorityGraceMs);
                var isInsidePriorityGraceWindow =
                    graceMs > 0
                    && DateTime.Now - _lastSpotifyTrackPlayIssuedAtUtc
                        < TimeSpan.FromMilliseconds(graceMs);

                if (
                    _spotifyConfiguration.PrioritizeUserPlayback
                    && isTrackChangedExternally
                    && playback.IsPlaying
                    && !isInsidePriorityGraceWindow
                )
                {
                    logger.LogInformation(
                        "Обнаружено ручное воспроизведение в Spotify (TrackId={TrackId}) - SoundRequest поставлен на паузу",
                        playback.TrackId ?? "null"
                    );

                    await stateManager.SetPlaybackStateAsync(PlaybackState.Paused, notify: true);
                    _lastSpotifyCompletedQueueItemId = null;
                }
                else if (isTrackChangedExternally || isTrackAlmostEnded)
                {
                    await _spotifyMonitorTransitionLock.WaitAsync(ct);
                    try
                    {
                        if (_lastSpotifyCompletedQueueItemId != currentQueueItem.Id)
                        {
                            _lastSpotifyCompletedQueueItemId = currentQueueItem.Id;
                            await OnTrackEndedAsync(currentQueueItem.Track);
                        }
                    }
                    finally
                    {
                        _spotifyMonitorTransitionLock.Release();
                    }
                }
            }
        }
        else
        {
            _lastSpotifyCompletedQueueItemId = null;
        }
    }

    private async Task<bool> IsSpotifyModeAsync(CancellationToken ct)
    {
        var provider = _soundRequestConfiguration.Provider;
        var result = false;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var providerState = await db
            .RootState.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Name == RootStateKeys.SoundRequestProvider, ct);

        if (
            providerState is { Value: not null }
            && TryParseProvider(providerState.Value, out var parsedProvider)
        )
        {
            provider = parsedProvider;
        }

        if (
            provider == SoundRequestProvider.Spotify
            && _spotifyConfiguration.Enabled
            && IsPlatformAllowed("Spotify")
        )
        {
            result = true;
        }

        return result;
    }

    private static bool TryParseProvider(string rawValue, out SoundRequestProvider provider)
    {
        var result = false;
        provider = SoundRequestProvider.YouTube;

        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            var normalizedValue = rawValue.Trim();
            if (Enum.TryParse<SoundRequestProvider>(normalizedValue, true, out var byName))
            {
                provider = byName;
                result = true;
            }
            else if (int.TryParse(normalizedValue, out var numericValue))
            {
                if (Enum.IsDefined(typeof(SoundRequestProvider), numericValue))
                {
                    provider = (SoundRequestProvider)numericValue;
                    result = true;
                }
            }
        }

        return result;
    }

    private bool IsPlatformAllowed(string platformName)
    {
        var result = false;
        var enabledPlatforms = _soundRequestConfiguration.EnabledPlatforms;

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

    #endregion

    #region Database Operations

    private async Task UpdateQueueItemLastPlayedAsync(QueueItem queueItem)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

            var dbTrack = await db.Tracks.FirstOrDefaultAsync(
                t => t.Id == queueItem.TrackId,
                _cancellationToken
            );

            if (dbTrack != null)
            {
                dbTrack.LastTimePlays = DateTime.Now;
                db.Tracks.Update(dbTrack);
                await db.SaveChangesAsync(_cancellationToken);
            }
        }
        catch (Exception)
        {
            // Non-critical
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _spotifyMonitorTransitionLock.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}
