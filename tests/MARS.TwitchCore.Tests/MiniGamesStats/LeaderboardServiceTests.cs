using MARS.TwitchCore.Data;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services;
using MARS.TwitchCore.Services.MiniGamesStats;
using MARS.TwitchCore.Tests.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.TwitchCore.Tests.MiniGamesStats;

/// <summary>
/// Таблица побед мини-игр поверх живой PostgreSQL.
/// </summary>
/// <remarks>
/// Сервис увеличивает счётчики через <c>ExecuteUpdateAsync</c>: на обходном
/// провайдере ради этого приходилось брать SQLite in-memory с удерживаемым
/// соединением. Теперь та же операция проверяется на сервере, на котором
/// сервис работает.
/// </remarks>
public class LeaderboardServiceTests : IDisposable
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly Mock<ITwitchUserEnsureService> _ensureUser = new();

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private LeaderboardService CreateService() =>
        new(_factory, _ensureUser.Object, NullLogger<LeaderboardService>.Instance);

    private async Task EnsureUserAsync(string twitchId)
    {
        var ct = TestContext.Current.CancellationToken;
        using var context = _factory.CreateDbContext();
        context.TwitchUsers.Add(
            new TwitchUser
            {
                TwitchId = twitchId,
                UserLogin = twitchId,
                DisplayName = twitchId,
            }
        );
        await context.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task RecordRouletteWinAsync_CreatesRowOnFirstWin()
    {
        var ct = TestContext.Current.CancellationToken;
        await EnsureUserAsync("42");
        _ensureUser
            .Setup(s => s.EnsureUserExistsAsync("42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TwitchUser
                {
                    TwitchId = "42",
                    UserLogin = "42",
                    DisplayName = "42",
                }
            );

        await CreateService().RecordRouletteWinAsync("42", false, ct);

        using var context = _factory.CreateDbContext();
        var stored = await context.TwitchLeaderboardUsers.SingleAsync(ct);

        Assert.Equal(1, stored.RussianRouletteWins);
        Assert.Equal(0, stored.RussianRouletteWinsWithWaifu);
        Assert.Equal(0, stored.TriviaWins);
    }

    /// <summary>
    /// Счётчики обязаны накапливаться, а не перезаписываться: каждая победа —
    /// это отдельное событие, и потеря предыдущих значений ломала бы таблицу
    /// лидеров.
    /// </summary>
    [Fact]
    public async Task RecordRouletteWinAsync_IncrementsExistingRow()
    {
        var ct = TestContext.Current.CancellationToken;
        await EnsureUserAsync("42");
        var service = CreateService();

        await service.RecordRouletteWinAsync("42", true, ct);
        await service.RecordRouletteWinAsync("42", false, ct);

        using var context = _factory.CreateDbContext();
        var stored = await context.TwitchLeaderboardUsers.SingleAsync(ct);

        Assert.Equal(2, stored.RussianRouletteWins);
        Assert.Equal(1, stored.RussianRouletteWinsWithWaifu);
    }

    [Fact]
    public async Task RecordTriviaWinAsync_CreatesRowOnFirstWin()
    {
        var ct = TestContext.Current.CancellationToken;
        await EnsureUserAsync("42");
        _ensureUser
            .Setup(s => s.EnsureUserExistsAsync("42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TwitchUser
                {
                    TwitchId = "42",
                    UserLogin = "42",
                    DisplayName = "42",
                }
            );

        await CreateService().RecordTriviaWinAsync("42", true, ct);

        using var context = _factory.CreateDbContext();
        var stored = await context.TwitchLeaderboardUsers.SingleAsync(ct);

        Assert.Equal(1, stored.TriviaWins);
        Assert.Equal(1, stored.TriviaWinsWithWaifus);
        Assert.Equal(0, stored.RussianRouletteWins);
    }

    [Fact]
    public async Task RecordTriviaWinAsync_IncrementsExistingRow()
    {
        var ct = TestContext.Current.CancellationToken;
        await EnsureUserAsync("42");
        var service = CreateService();

        await service.RecordTriviaWinAsync("42", false, ct);
        await service.RecordTriviaWinAsync("42", true, ct);

        using var context = _factory.CreateDbContext();
        var stored = await context.TwitchLeaderboardUsers.SingleAsync(ct);

        Assert.Equal(2, stored.TriviaWins);
        Assert.Equal(1, stored.TriviaWinsWithWaifus);
    }

    /// <summary>
    /// Порядок таблицы лидеров из монолита: сначала по сумме побед, затем по
    /// победам «с женой». Без второго ключа игроки с равным счётом шли бы в
    /// произвольном порядке и таблица мерцала бы при каждом обновлении.
    /// </summary>
    [Fact]
    public async Task GetTopAsync_OrdersByTotalWinsThenByWithWaifu()
    {
        var ct = TestContext.Current.CancellationToken;
        await EnsureUserAsync("1");
        await EnsureUserAsync("2");
        await EnsureUserAsync("3");

        using (var context = _factory.CreateDbContext())
        {
            context.TwitchLeaderboardUsers.AddRange(
                new TwitchLeaderboardUser { TwitchId = "1", RussianRouletteWins = 1 },
                new TwitchLeaderboardUser { TwitchId = "2", TriviaWins = 2 },
                new TwitchLeaderboardUser
                {
                    TwitchId = "3",
                    TriviaWins = 2,
                    TriviaWinsWithWaifus = 1,
                }
            );
            await context.SaveChangesAsync(ct);
        }

        var top = await CreateService().GetTopAsync(3, ct);

        Assert.True(top.Success);
        Assert.Equal(["3", "2", "1"], top.Result!.Select(u => u.TwitchId));
    }

    [Fact]
    public async Task GetTopAsync_RespectsRequestedCount()
    {
        var ct = TestContext.Current.CancellationToken;
        await EnsureUserAsync("4");
        await EnsureUserAsync("5");

        using (var context = _factory.CreateDbContext())
        {
            context.TwitchLeaderboardUsers.AddRange(
                new TwitchLeaderboardUser { TwitchId = "4", TriviaWins = 1 },
                new TwitchLeaderboardUser { TwitchId = "5", TriviaWins = 5 }
            );
            await context.SaveChangesAsync(ct);
        }

        var top = await CreateService().GetTopAsync(1, ct);

        Assert.True(top.Success);
        Assert.Equal(["5"], top.Result!.Select(u => u.TwitchId));
    }

    [Fact]
    public async Task GetUserStatsAsync_ReturnsPlaceAndRow()
    {
        var ct = TestContext.Current.CancellationToken;
        await EnsureUserAsync("6");
        await EnsureUserAsync("7");

        using (var context = _factory.CreateDbContext())
        {
            context.TwitchLeaderboardUsers.AddRange(
                new TwitchLeaderboardUser { TwitchId = "6", RussianRouletteWins = 5 },
                new TwitchLeaderboardUser { TwitchId = "7", RussianRouletteWins = 1 }
            );
            await context.SaveChangesAsync(ct);
        }

        var stats = await CreateService().GetUserStatsAsync("7", ct);

        Assert.True(stats.Success);
        Assert.Equal(2, stats.Result!.Place);
        Assert.NotNull(stats.Result.User);
        Assert.Equal("7", stats.Result.User!.TwitchId);
    }

    /// <summary>
    /// Неизвестный пользователь — это «нет статистики», а не ошибка: место 0 и
    /// пустая строка в таблице лидеров.
    /// </summary>
    [Fact]
    public async Task GetUserStatsAsync_ReturnsZeroPlaceForUnknownUser()
    {
        var ct = TestContext.Current.CancellationToken;

        var stats = await CreateService().GetUserStatsAsync("999999", ct);

        Assert.True(stats.Success);
        Assert.Equal(0, stats.Result!.Place);
        Assert.Null(stats.Result.User);
    }
}
