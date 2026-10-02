using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.Shared.Clients;
using Moq;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Команды таблицы лидеров. Данные лежат в MARS.TwitchCore, поэтому проверяется
/// разбор ответа клиента и тексты результата, а не сам сервис.
/// </summary>
public class LeaderboardCommandsTests
{
    private static Mock<ILeaderboardClient> BuildClient(
        LeaderboardTop? top,
        LeaderboardStats? stats
    )
    {
        var client = new Mock<ILeaderboardClient>();
        client
            .Setup(c => c.GetTopAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(top);
        client
            .Setup(c => c.GetUserStatsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        return client;
    }

    private static Dictionary<string, object> NoParameters() => new();

    [Fact]
    public async Task MGLeaders_NumbersTheEntries()
    {
        var ct = TestContext.Current.CancellationToken;
        var top = new LeaderboardTop(
            [new LeaderboardEntry("1", "Первый", 9, 4, 5), new LeaderboardEntry("2", null, 3, 3, 0)]
        );

        var command = new MGLeadersCommand(BuildClient(top, null).Object);

        var result = await command.ExecuteAsync(NoParameters(), cancellationToken: ct);

        Assert.True(result.Success);
        Assert.Contains("1. Первый — 9 побед", result.Text);
        Assert.Contains("2. 2 — 3 побед", result.Text);
    }

    /// <summary>
    /// Имя может не прийти: внешний ключ на TwitchUser необязателен, и в ответе
    /// вместо него остаётся id.
    /// </summary>
    [Fact]
    public async Task MGLeaders_FallsBackToTwitchId()
    {
        var ct = TestContext.Current.CancellationToken;
        var top = new LeaderboardTop([new LeaderboardEntry("77", null, 1, 1, 0)]);

        var command = new MGLeadersCommand(BuildClient(top, null).Object);

        var result = await command.ExecuteAsync(NoParameters(), cancellationToken: ct);

        Assert.Contains("1. 77 — 1 побед", result.Text);
    }

    [Fact]
    public async Task MGLeaders_ReportsEmptyTopAsSuccess()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new MGLeadersCommand(BuildClient(new LeaderboardTop([]), null).Object);

        var result = await command.ExecuteAsync(NoParameters(), cancellationToken: ct);

        Assert.True(result.Success);
        Assert.Contains("отсутствуют", result.Text);
    }

    /// <summary>
    /// null от клиента — это «сервис недоступен», а не «топ пуст»: команда
    /// обязана сказать об этом явно, иначе зритель решит, что никто не выиграл.
    /// </summary>
    [Fact]
    public async Task MGLeaders_ReportsUnreachableService()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new MGLeadersCommand(BuildClient(null, null).Object);

        var result = await command.ExecuteAsync(NoParameters(), cancellationToken: ct);

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.TargetUnreachable, result.ErrorCode);
    }

    [Fact]
    public async Task MyWins_ReportsPlaceAndWins()
    {
        var ct = TestContext.Current.CancellationToken;
        var stats = new LeaderboardStats(4, new LeaderboardEntry("5", "Пятый", 11, 6, 5));

        var command = new MyWinsCommand(BuildClient(null, stats).Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["userId"] = "5" },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        Assert.Contains("@Пятый", result.Text);
        Assert.Contains("4 месте", result.Text);
        Assert.Contains("11", result.Text);
    }

    /// <summary>
    /// Twitch-адаптер передаёт объект пользователя, а не строку. Без рефлексии
    /// по <c>TwitchId</c> команда работала бы только на API и Telegram.
    /// </summary>
    [Fact]
    public async Task MyWins_ReadsTheUserObjectPassedByTheTwitchAdapter()
    {
        var ct = TestContext.Current.CancellationToken;
        var stats = new LeaderboardStats(1, new LeaderboardEntry("5", "Пятый", 2, 1, 1));
        var client = BuildClient(null, stats);
        client
            .Setup(c => c.GetUserStatsAsync("5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var command = new MyWinsCommand(client.Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["user"] = new StubTwitchUser("5") },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        client.Verify(c => c.GetUserStatsAsync("5", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MyWins_ReportsNoWinsAsSuccess()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new MyWinsCommand(BuildClient(null, new LeaderboardStats(0, null)).Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["userId"] = "5" },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        Assert.Contains("нет побед", result.Text);
    }

    [Fact]
    public async Task MyWins_Fails_WhenCallerIsUnknown()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new MyWinsCommand(BuildClient(null, null).Object);

        var result = await command.ExecuteAsync(NoParameters(), cancellationToken: ct);

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.BadArguments, result.ErrorCode);
    }

    private sealed record StubTwitchUser(string TwitchId);
}
