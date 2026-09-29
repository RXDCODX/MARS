using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Читает и кэширует кулдауны роллов из <c>waifu.RootState</c>.
/// Заменяет зашитые 20 минут в <see cref="WaifuRollService"/> и дублирующий
/// (и всегда промахивающийся из-за префикса <c>RootState_</c>) запрос в
/// <see cref="RollCooldownNotificationService"/>.
/// </summary>
public sealed class RollCooldownConfigurationService(
    IDbContextFactory<WaifuDbContext> dbContextFactory,
    ILogger<RollCooldownConfigurationService> logger
)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Dictionary<string, long> _cache = new(StringComparer.Ordinal);
    private DateTime _cachedAtUtc = DateTime.MinValue;

    /// <summary>Кулдаун ролла указанного типа; при отсутствии/ошибке — 20 минут.</summary>
    public async Task<TimeSpan> GetCooldownAsync(
        string rollType,
        CancellationToken cancellationToken = default
    )
    {
        var key = RootStateKeys.RollCooldownKey(rollType);
        var minutes = await GetMinutesAsync(key, cancellationToken);

        return TimeSpan.FromMinutes(minutes);
    }

    private async Task<long> GetMinutesAsync(string key, CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        var minutes = snapshot.TryGetValue(key, out var configured) && configured > 0 ? configured : 20;

        return minutes;
    }

    private async Task<Dictionary<string, long>> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _cachedAtUtc < CacheLifetime)
        {
            return _cache;
        }

        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            if (DateTime.UtcNow - _cachedAtUtc >= CacheLifetime)
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

                var rows = await dbContext
                    .RootState.AsNoTracking()
                    .Select(state => new { state.Name, state.Value })
                    .ToListAsync(cancellationToken);

                var parsed = new Dictionary<string, long>(StringComparer.Ordinal);

                foreach (var row in rows)
                {
                    if (long.TryParse(row.Value, out var minutes) && minutes > 0)
                    {
                        parsed[row.Name] = minutes;
                    }
                }

                _cache = parsed;
                _cachedAtUtc = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось прочитать кулдауны роллов, используются значения по умолчанию"
            );
        }
        finally
        {
            _refreshLock.Release();
        }

        return _cache;
    }
}
