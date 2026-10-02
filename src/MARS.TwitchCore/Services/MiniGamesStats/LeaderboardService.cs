using MARS.Shared.Models;
using MARS.TwitchCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.TwitchCore.Services.MiniGamesStats;

/// <summary>
/// Таблица лидеров мини-игр.
/// </summary>
/// <remarks>
/// Счётчики обновляются через <c>ExecuteUpdateAsync</c>: инкремент выполняется
/// в базе, поэтому параллельные победы одного игрока не затирают друг друга.
/// Отличие от монолита — ошибка возвращается, а не проглатывается внутри
/// <c>catch</c>: вызывающая сторона решает, что важнее — потерянная победа или
/// ответ пользователю.
/// </remarks>
public sealed class LeaderboardService(
    IDbContextFactory<Data.TwitchDbContext> factory,
    ITwitchUserEnsureService twitchUserEnsureService,
    ILogger<LeaderboardService> logger
) : ILeaderboardService
{
    public Task<OperationResult> RecordRouletteWinAsync(
        string twitchId,
        bool withWaifu,
        CancellationToken cancellationToken = default
    )
    {
        return RecordWinAsync(twitchId, withWaifu, MiniGame.RussianRoulette, cancellationToken);
    }

    public Task<OperationResult> RecordTriviaWinAsync(
        string twitchId,
        bool withWaifu,
        CancellationToken cancellationToken = default
    )
    {
        return RecordWinAsync(twitchId, withWaifu, MiniGame.Trivia, cancellationToken);
    }

    /// <summary>
    /// Топ по сумме побед; при равенстве — по победам «с женой».
    /// </summary>
    public async Task<OperationResult<IReadOnlyList<TwitchLeaderboardUser>>> GetTopAsync(
        int count,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Fail(
            "Стартовая ошибка чтения таблицы лидеров"
        );

        if (count > 0)
        {
            try
            {
                await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

                var top = await dbContext
                    .TwitchLeaderboardUsers.AsNoTracking()
                    .Include(e => e.TwitchUser)
                    .OrderByDescending(e => e.RussianRouletteWins + e.TriviaWins)
                    .ThenByDescending(e => e.RussianRouletteWinsWithWaifu + e.TriviaWinsWithWaifus)
                    .Take(count)
                    .ToListAsync(cancellationToken);

                result = OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Ok(top);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось получить топ победителей мини-игр");
                result = OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Fail(
                    $"Не удалось получить топ победителей мини-игр: {ex.Message}"
                );
            }
        }
        else
        {
            result = OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Fail(
                "Запрошенное число строк должно быть больше нуля"
            );
        }

        return result;
    }

    /// <summary>
    /// Место считается как «сколько игроков набрали больше» + 1. Ранжирующая
    /// оконная функция <c>RANK</c> дала бы двум игрокам с одинаковым счётом
    /// разные номера одной позиции — <c>DENSE_RANK</c> тут не нужен, потому что
    /// сравнение идёт только по «больше».
    /// </summary>
    public async Task<OperationResult<LeaderboardUserStats>> GetUserStatsAsync(
        string twitchId,
        CancellationToken cancellationToken = default
    )
    {
        var result = OperationResult<LeaderboardUserStats>.Fail(
            "Стартовая ошибка чтения статистики мини-игр"
        );

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

            var user = await dbContext
                .TwitchLeaderboardUsers.AsNoTracking()
                .Include(e => e.TwitchUser)
                .FirstOrDefaultAsync(e => e.TwitchId == twitchId, cancellationToken);

            if (user is not null)
            {
                var better = await dbContext
                    .TwitchLeaderboardUsers.AsNoTracking()
                    .CountAsync(
                        e =>
                            e.RussianRouletteWins + e.TriviaWins
                            > user.RussianRouletteWins + user.TriviaWins,
                        cancellationToken
                    );

                result = OperationResult<LeaderboardUserStats>.Ok(
                    new LeaderboardUserStats(better + 1, user)
                );
            }
            else
            {
                result = OperationResult<LeaderboardUserStats>.Ok(
                    new LeaderboardUserStats(0, null)
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось получить статистику мини-игр для {TwitchId}", twitchId);
            result = OperationResult<LeaderboardUserStats>.Fail(
                $"Не удалось получить статистику мини-игр: {ex.Message}"
            );
        }

        return result;
    }

    /// <summary>
    /// Запись победы. Существующая строка обновляется инкрементом в базе,
    /// иначе создаётся новая — с предварительным созданием пользователя, потому
    /// что внешний ключ на <c>TwitchUser</c> не даёт вставить строку лидера для
    /// несуществующего пользователя.
    /// </summary>
    private async Task<OperationResult> RecordWinAsync(
        string twitchId,
        bool withWaifu,
        MiniGame game,
        CancellationToken cancellationToken
    )
    {
        var result = OperationResult.Fail("Стартовая ошибка записи победы");

        try
        {
            await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);

            var existing = await dbContext.TwitchLeaderboardUsers.FirstOrDefaultAsync(
                e => e.TwitchId == twitchId,
                cancellationToken
            );

            if (existing is not null)
            {
                if (game == MiniGame.RussianRoulette)
                {
                    await dbContext
                        .TwitchLeaderboardUsers.Where(e => e.TwitchId == twitchId)
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(
                                        e => e.RussianRouletteWins,
                                        e => e.RussianRouletteWins + 1
                                    )
                                    .SetProperty(
                                        e => e.RussianRouletteWinsWithWaifu,
                                        e => e.RussianRouletteWinsWithWaifu + (withWaifu ? 1 : 0)
                                    ),
                            cancellationToken
                        );
                }
                else
                {
                    await dbContext
                        .TwitchLeaderboardUsers.Where(e => e.TwitchId == twitchId)
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(e => e.TriviaWins, e => e.TriviaWins + 1)
                                    .SetProperty(
                                        e => e.TriviaWinsWithWaifus,
                                        e => e.TriviaWinsWithWaifus + (withWaifu ? 1 : 0)
                                    ),
                            cancellationToken
                        );
                }
            }
            else
            {
                await twitchUserEnsureService.EnsureUserExistsAsync(twitchId, cancellationToken);

                var row = new TwitchLeaderboardUser { TwitchId = twitchId };

                if (game == MiniGame.RussianRoulette)
                {
                    row.RussianRouletteWins = 1;
                    row.RussianRouletteWinsWithWaifu = withWaifu ? 1 : 0;
                }
                else
                {
                    row.TriviaWins = 1;
                    row.TriviaWinsWithWaifus = withWaifu ? 1 : 0;
                }

                dbContext.TwitchLeaderboardUsers.Add(row);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            result = OperationResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Не удалось записать победу в {Game} для {TwitchId}",
                game,
                twitchId
            );
            result = OperationResult.Fail($"Не удалось записать победу в {game}: {ex.Message}");
        }

        return result;
    }
}
