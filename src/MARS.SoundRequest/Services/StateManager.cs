using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace MARS.SoundRequest.Services;

/// <summary>
/// Менеджер состояния плеера с поддержкой многопоточности и персистентностью в БД
/// </summary>
public class StateManager(
    IDbContextFactory<MediaDbContext> dbFactory,
    IHostApplicationLifetime lifetime,
    ILogger<StateManager> logger
) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private PlayerState _currentState = new()
    {
        Id = Guid.NewGuid(),
        Volume = 100f,
        State = PlaybackState.Stopped,
        IsMuted = false,
        PausedByMute = false,
    };
    private bool _disposed;
    private bool _isInitialized;

    /// <summary>
    /// Аудит: <c>GetState()</c> делал <c>_semaphore.Wait()</c>, то есть блокировал
    /// поток. Вместо этого под локом публикуется иммутабельный снапшот, и синхронный
    /// getter просто читает ссылку — без блокировки и без гонки.
    /// </summary>
    private PlayerState _stateSnapshot = new()
    {
        Id = Guid.NewGuid(),
        Volume = 100f,
        State = PlaybackState.Stopped,
        IsMuted = false,
        PausedByMute = false,
    };

    /// <summary>
    /// Событие изменения состояния плеера
    /// </summary>
    public event Func<PlayerState, string?, Task>? StateChanged;

    /// <summary>
    /// Инициализация состояния из БД (вызывается один раз при старте)
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _semaphore.WaitAsync(_cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

            var dbState = await db
                .PlayerStates.AsNoTracking()
                .Include(s => s.CurrentQueueItem)
                .ThenInclude(qi => qi!.Track)
                .SingleOrDefaultAsync(_cancellationToken);

            if (dbState != null)
            {
                if (dbState.State == PlaybackState.Stopped)
                {
                    dbState.CurrentTrackProgress = TimeSpan.Zero;
                }

                logger.LogInformation(
                    "Загружено состояние плеера из БД: ID={StateId}, State={State}, Volume={Volume}",
                    dbState.Id,
                    dbState.State,
                    dbState.Volume
                );
                _currentState = dbState;
            }
            else
            {
                logger.LogInformation("Состояние плеера не найдено в БД, создаем новое");
                db.PlayerStates.Add(_currentState);
                await db.SaveChangesAsync(_cancellationToken);
            }

            PublishSnapshot();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Ошибка при инициализации состояния плеера из БД, используем состояние по умолчанию"
            );
            _isInitialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PlayerState> GetStateAsync()
    {
        return CloneState(_stateSnapshot);
    }

    /// <summary>
    /// Синхронное чтение состояния. Аудит: раньше здесь был
    /// <c>_semaphore.Wait()</c> — блокировка потока. Теперь состояние отдаётся из
    /// опубликованного снапшота без ожидания семафора.
    /// </summary>
    public PlayerState GetState()
    {
        return CloneState(_stateSnapshot);
    }

    private void PublishSnapshot()
    {
        _stateSnapshot = CloneState(_currentState);
    }

    private async Task SaveStateToDbAsync(PlayerState stateToSave)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

            var existingState = await db.PlayerStates.FindAsync(
                [stateToSave.Id],
                cancellationToken: _cancellationToken
            );

            if (existingState != null)
            {
                existingState.CurrentQueueItemId = stateToSave.CurrentQueueItemId;
                existingState.CurrentTrackProgress = stateToSave.CurrentTrackProgress;
                existingState.State = stateToSave.State;
                existingState.VideoState = stateToSave.VideoState;
                existingState.IsMuted = stateToSave.IsMuted;
                existingState.PausedByMute = stateToSave.PausedByMute;
                existingState.Volume = stateToSave.Volume;

                db.PlayerStates.Update(existingState);
            }
            else
            {
                db.PlayerStates.Add(stateToSave);
            }

            await db.SaveChangesAsync(_cancellationToken);

            logger.LogDebug(
                "Состояние плеера сохранено в БД: State={State}, Volume={Volume}",
                stateToSave.State,
                stateToSave.Volume
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при сохранении состояния плеера в БД");
        }
    }

    private async Task UpdateStateAsync(
        Action<PlayerState> updateAction,
        bool notifyStateChanged = true,
        string? excludeSubscriberId = null
    )
    {
        PlayerState? stateToNotify = null;

        // Аудит: раньше лок освобождался до SaveStateToDbAsync, и две параллельные
        // правки могли записать в БД смесь полей из разных обновлений. Теперь
        // мутация, снапшот и сохранение выполняются под одним семафором, а
        // сохранение идёт по снятому снапшоту, а не по живым полям.
        await _semaphore.WaitAsync(_cancellationToken);
        try
        {
            updateAction(_currentState);
            _currentState.StateVersion = Guid.NewGuid();

            var snapshot = CloneState(_currentState);
            _stateSnapshot = snapshot;

            await SaveStateToDbAsync(snapshot);

            if (notifyStateChanged)
            {
                stateToNotify = CloneState(snapshot);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        if (stateToNotify != null && StateChanged != null)
        {
            await StateChanged.Invoke(stateToNotify, excludeSubscriberId);
        }
    }

    public async Task SetCurrentQueueItemAsync(
        QueueItem? queueItem,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(
            state =>
            {
                state.CurrentQueueItemId = queueItem?.Id;
                state.CurrentQueueItem = queueItem;
                state.State = PlaybackState.WaitingForTrack;
            },
            notify,
            excludeSubscriberId
        );
    }

    public async Task SetPlaybackStateAsync(
        PlaybackState playbackState,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(
            state =>
            {
                state.State = playbackState;
                if (playbackState == PlaybackState.Stopped)
                {
                    state.CurrentQueueItemId = null;
                    state.CurrentQueueItem = null;
                    state.CurrentTrackProgress = null;
                }
            },
            notify,
            excludeSubscriberId
        );
    }

    public async Task SetPausedAsync(
        bool isPaused,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await SetPlaybackStateAsync(
            isPaused ? PlaybackState.Paused : PlaybackState.Playing,
            notify,
            excludeSubscriberId
        );
    }

    public async Task SetMutedAsync(
        bool isMuted,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(state => state.IsMuted = isMuted, notify, excludeSubscriberId);
    }

    public async Task SetPausedByMuteAsync(
        bool byMute,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(state => state.PausedByMute = byMute, notify, excludeSubscriberId);
    }

    public async Task SetVolumeAsync(
        float volume,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(
            state => state.Volume = Math.Clamp(volume, 0f, 100f),
            notify,
            excludeSubscriberId
        );
    }

    public async Task SetVideoDisplayAsync(
        VideoDisplay videoDisplay,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(
            state => state.VideoState = videoDisplay,
            notify,
            excludeSubscriberId
        );
    }

    public async Task UpdateCurrentTrackProgressAsync(
        TimeSpan progress,
        bool notify = false,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(
            state =>
            {
                if (state.CurrentQueueItem != null)
                {
                    state.CurrentTrackProgress = progress;
                }
            },
            notify,
            excludeSubscriberId
        );
    }

    public async Task StartPlayingAsync(
        QueueItem queueItem,
        bool notify = true,
        string? excludeSubscriberId = null
    )
    {
        await UpdateStateAsync(
            state =>
            {
                state.CurrentQueueItemId = queueItem.Id;
                state.CurrentQueueItem = queueItem;
                state.State = PlaybackState.Playing;
                state.CurrentTrackProgress = TimeSpan.Zero;
            },
            notify,
            excludeSubscriberId
        );
    }

    public async Task StopPlaybackAsync(bool notify = true, string? excludeSubscriberId = null)
    {
        await UpdateStateAsync(
            state =>
            {
                state.CurrentQueueItemId = null;
                state.CurrentQueueItem = null;
                state.CurrentTrackProgress = null;
                state.State = PlaybackState.Stopped;
            },
            notify,
            excludeSubscriberId
        );
    }

    public async Task NotifyStateChangedAsync(string? excludeSubscriberId = null)
    {
        var state = await GetStateAsync();
        if (StateChanged != null)
        {
            await StateChanged.Invoke(state, excludeSubscriberId);
        }
    }

    public async Task UpdateStateAsync(Action<PlayerState> update)
    {
        await UpdateStateAsync(update, notifyStateChanged: true);
    }

    private async Task<QueueItem?> GetNextQueueItemAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync(_cancellationToken);

        return await db
            .QueueItems.AsNoTracking()
            .Include(qi => qi.Track)
            .FirstOrDefaultAsync(qi => qi.QueueOrder == 1, _cancellationToken);
    }

    private static PlayerState CloneState(PlayerState source)
    {
        return new PlayerState
        {
            Id = source.Id,
            State = source.State,
            IsMuted = source.IsMuted,
            Volume = source.Volume,
            VideoState = source.VideoState,
            CurrentTrackProgress = source.CurrentTrackProgress,
            CurrentQueueItemId = source.CurrentQueueItemId,
            CurrentQueueItem = source.CurrentQueueItem,
            PausedByMute = source.PausedByMute,
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
