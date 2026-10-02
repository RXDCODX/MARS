using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.TwitchCore.Controllers;
using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.MiniGamesStats;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using TwitchUserEntity = MARS.TwitchCore.Entities.TwitchUser;

namespace MARS.TwitchCore.Tests.Leaderboard;

/// <summary>
/// Внутренний API таблицы лидеров: контроллер отдаёт конверт
/// <see cref="OperationResult{T}"/> и различает «пусто» и «ошибка».
/// </summary>
public class LeaderboardControllerTests
{
    private static ControllerContext Context() => new() { HttpContext = new DefaultHttpContext() };

    private static LeaderboardController Build(
        OperationResult<IReadOnlyList<TwitchLeaderboardUser>> top,
        OperationResult<LeaderboardUserStats> stats
    )
    {
        var service = new Mock<ILeaderboardService>();
        service
            .Setup(s => s.GetTopAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(top);
        service
            .Setup(s => s.GetUserStatsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        return new LeaderboardController(service.Object) { ControllerContext = Context() };
    }

    private static TwitchLeaderboardUser Row(string twitchId, string? displayName, int wins) =>
        new()
        {
            TwitchId = twitchId,
            RussianRouletteWins = wins,
            TwitchUser = displayName is null
                ? null
                : new TwitchUserEntity
                {
                    TwitchId = twitchId,
                    UserLogin = twitchId,
                    DisplayName = displayName,
                },
        };

    [Fact]
    public async Task GetTop_MapsRowsToTheSharedContract()
    {
        var controller = Build(
            OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Ok([Row("1", "Первый", 7)]),
            OperationResult<LeaderboardUserStats>.Ok(new LeaderboardUserStats(0, null))
        );

        var result = Assert.IsType<OkObjectResult>(
            ((IConvertToActionResult)await controller.GetTop()).Convert()
        );
        var envelope = result.Value as OperationResult<LeaderboardTop>;

        Assert.True(envelope!.Success);
        var entry = Assert.Single(envelope!.Result!.Entries);
        Assert.Equal("1", entry.TwitchId);
        Assert.Equal("Первый", entry.DisplayName);
        Assert.Equal(7, entry.TotalWins);
    }

    /// <summary>
    /// Имя пользователя может отсутствовать: внешний ключ на TwitchUser
    /// необязателен, и клиент должен получить id, а не исключение.
    /// </summary>
    [Fact]
    public async Task GetTop_FallsBackToTwitchId_WithoutDisplayName()
    {
        var controller = Build(
            OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Ok([Row("1", null, 3)]),
            OperationResult<LeaderboardUserStats>.Ok(new LeaderboardUserStats(0, null))
        );

        var result = Assert.IsType<OkObjectResult>(
            ((IConvertToActionResult)await controller.GetTop()).Convert()
        );
        var envelope = result.Value as OperationResult<LeaderboardTop>;

        Assert.Null(Assert.Single(envelope!.Result!.Entries).DisplayName);
    }

    /// <summary>
    /// Бизнес-ошибка — это <c>Ok</c> с <c>Success = false</c>, а не
    /// <c>BadRequest</c>: вызывающая сторона различает «таблица пуста» и
    /// «сервис не ответил» по конверту, а не по коду ответа.
    /// </summary>
    [Fact]
    public async Task GetTop_ReportsServiceFailureInsideTheEnvelope()
    {
        var controller = Build(
            OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Fail("база недоступна"),
            OperationResult<LeaderboardUserStats>.Ok(new LeaderboardUserStats(0, null))
        );

        var result = Assert.IsType<OkObjectResult>(
            ((IConvertToActionResult)await controller.GetTop()).Convert()
        );
        var envelope = result.Value as OperationResult<LeaderboardTop>;

        Assert.False(envelope!.Success);
        Assert.Equal("база недоступна", envelope!.ErrorMessage);
    }

    [Fact]
    public async Task GetUser_ReturnsPlaceAndRow()
    {
        var controller = Build(
            OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Ok([]),
            OperationResult<LeaderboardUserStats>.Ok(
                new LeaderboardUserStats(3, Row("9", "Девятый", 2))
            )
        );

        var result = Assert.IsType<OkObjectResult>(
            ((IConvertToActionResult)await controller.GetUser("9")).Convert()
        );
        var envelope = result.Value as OperationResult<LeaderboardStats>;

        Assert.True(envelope!.Success);
        Assert.Equal(3, envelope!.Result!.Place);
        Assert.Equal("9", envelope.Result.User!.TwitchId);
    }

    /// <summary>
    /// «Нет статистики» — это успех с пустой строкой, а не ошибка: так клиент
    /// команды отличает новичка от недоступного сервиса.
    /// </summary>
    [Fact]
    public async Task GetUser_ReportsUnknownPlayerAsEmptySuccess()
    {
        var controller = Build(
            OperationResult<IReadOnlyList<TwitchLeaderboardUser>>.Ok([]),
            OperationResult<LeaderboardUserStats>.Ok(new LeaderboardUserStats(0, null))
        );

        var result = Assert.IsType<OkObjectResult>(
            ((IConvertToActionResult)await controller.GetUser("404")).Convert()
        );
        var envelope = result.Value as OperationResult<LeaderboardStats>;

        Assert.True(envelope!.Success);
        Assert.Equal(0, envelope!.Result!.Place);
        Assert.Null(envelope.Result.User);
    }
}
