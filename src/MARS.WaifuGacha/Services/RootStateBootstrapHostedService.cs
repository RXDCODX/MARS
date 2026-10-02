using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

/// <summary>
/// Создаёт отсутствующие ключи <c>waifu.RootState</c> при старте сервиса.
/// До этого их создавал только MARS.Admin в схеме <c>admin</c>, а WaifuGacha читал
/// пустую <c>waifu.RootState</c>, из-за чего конфигурация кулдаунов была мёртвой.
/// </summary>
public sealed class RootStateBootstrapHostedService(
    IDbContextFactory<WaifuDbContext> dbContextFactory,
    ILogger<RootStateBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            var existing = await dbContext
                .RootState.AsNoTracking()
                .Select(state => state.Name)
                .ToListAsync(cancellationToken);
            var existingSet = existing.ToHashSet(StringComparer.Ordinal);

            var missing = RootStateKeys
                .Defaults.Where(pair => !existingSet.Contains(pair.Key))
                .Select(pair => new RootState
                {
                    Name = pair.Key,
                    Value = pair.Value.Value,
                    Description = pair.Value.Description,
                    TypeDescription = "long",
                })
                .ToList();

            if (missing.Count > 0)
            {
                await dbContext.RootState.AddRangeAsync(missing, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Создано {Count} ключей waifu.RootState: {Keys}",
                    missing.Count,
                    string.Join(", ", missing.Select(state => state.Name))
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при инициализации waifu.RootState");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var result = Task.CompletedTask;
        return result;
    }
}
