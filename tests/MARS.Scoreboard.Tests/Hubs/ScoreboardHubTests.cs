using MARS.Scoreboard.Data;
using MARS.Scoreboard.Hubs;
using MARS.Shared.Grpc.Scoreboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScoreboardService = MARS.Scoreboard.Services.ScoreboardService;
using TestDbContextFactory = MARS.Scoreboard.Tests.Grpc.TestDbContextFactory;

namespace MARS.Scoreboard.Tests.Hubs;

/// <summary>
/// Проверяет, что вызовы панели администратора меняют то же состояние, что и
/// REST-контроллер и gRPC-сервис.
/// </summary>
/// <remarks>
/// Отдельная проверка нужна потому, что эти методы — единственное, что было
/// добавлено в хаб: до них браузер слал <c>UpdateState</c> и
/// <c>SetVisibility</c> в путь, который не был объявлен в Gateway, и получал
/// 405. Тест ломается, если метод перестанет доходить до сервиса или начнёт
/// возвращать константу вместо его ответа.
/// <para>
/// Сервис настоящий и работает на живой PostgreSQL, а не на заглушке:
/// <c>ScoreboardService</c> не делает виртуальных методов, подменить его нечем,
/// а проверять делегирование через мок означало бы проверять мок. Заодно это
/// закрывает те самые пути, по которым состояние читается обратно.
/// </para>
/// </remarks>
public class ScoreboardHubTests : IAsyncLifetime
{
    private readonly TestDbContextFactory _factory;
    private ScoreboardService _service = null!;

    public ScoreboardHubTests()
    {
        _factory = new TestDbContextFactory();
    }

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IDbContextFactory<ScoreboardDbContext>>(_factory);
        services.AddScoped<ScoreboardService>();
        var provider = services.BuildServiceProvider();

        _service = provider.GetRequiredService<ScoreboardService>();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Снимок проходит через маппер, поэтому нужен полный набор полей.
    /// Игроки обязательны: <c>UpdatePlayerScoreAsync</c> и
    /// <c>SetPlayerFinalAsync</c> правят строку игрока и возвращают false, если
    /// её нет.
    /// </summary>
    private static ScoreboardSnapshot BuildSnapshot(
        bool isVisible = true,
        int animationDuration = 800
    ) =>
        new()
        {
            IsVisible = isVisible,
            AnimationDuration = animationDuration,
            Player1 = new ScoreboardPlayer { Name = "Первый", Score = 1 },
            Player2 = new ScoreboardPlayer { Name = "Второй", Score = 2 },
        };

    [Fact]
    public async Task UpdateStateAsync_AppliesVisibilityToStoredState()
    {
        var hub = new ScoreboardHub(_service);

        await hub.UpdateState(BuildSnapshot(isVisible: true, animationDuration: 1200));

        var stored = await _service.GetCurrentStateAsync();

        Assert.True(stored.IsVisible);
        Assert.Equal(1200, stored.AnimationDuration);
    }

    [Fact]
    public async Task SetVisibilityAsync_ReturnsServiceResult()
    {
        var hub = new ScoreboardHub(_service);

        var hidden = await hub.SetVisibility(false);

        Assert.True(hidden);
        Assert.False((await _service.GetCurrentStateAsync()).IsVisible);
    }

    [Fact]
    public async Task UpdatePlayerScoreAsync_ReturnsServiceResult()
    {
        var hub = new ScoreboardHub(_service);
        await hub.UpdateState(BuildSnapshot());

        var updated = await hub.UpdatePlayerScore(1, 17);

        Assert.True(updated);
        Assert.Equal(17, (await _service.GetCurrentStateAsync()).Player1.Score);
    }

    [Fact]
    public async Task SetPlayerFinalAsync_ReturnsServiceResult()
    {
        var hub = new ScoreboardHub(_service);
        await hub.UpdateState(BuildSnapshot());

        var updated = await hub.SetPlayerFinal(1, "Победа");

        Assert.True(updated);
        Assert.Equal("Победа", (await _service.GetCurrentStateAsync()).Player1.Final);
    }

    [Fact]
    public async Task CommandsRejectUnknownPlayerPosition()
    {
        var hub = new ScoreboardHub(_service);

        var scoreResult = await hub.UpdatePlayerScore(9, 5);
        var finalResult = await hub.SetPlayerFinal(9, "Победа");

        Assert.False(scoreResult);
        Assert.False(finalResult);
    }
}
