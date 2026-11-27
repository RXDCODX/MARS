using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Twitch;
using MARS.Platform.Infrastructure.Grpc.WaifuGacha;
using MARS.Shared.Contracts.Clients;
using MARS.Shared.Contracts.Twitch.Rest;
using Moq;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Команды инвентаря коллекций: коллекция лежит в <c>MARS.WaifuGacha</c>, а
/// логин в Twitch ID превращает справочник <c>MARS.TwitchCore</c>.
/// </summary>
public class CollectionInventoryCommandsTests
{
    private static CollectionInventory Inventory() =>
        new(2, 40, [new CollectionItem("Фумо Хина", 3), new CollectionItem("Фумо Бэби", 1)]);

    private static Mock<IMarsGrpcClient> BuildGrpcClient(
        CollectionInventory? inventory,
        string? twitchId
    )
    {
        var grpc = new Mock<IMarsGrpcClient>();

        var waifu = new Mock<IWaifuGachaApi>();
        waifu
            .Setup(c => c.GetFumoInventoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        waifu
            .Setup(c => c.GetMikuInventoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var twitch = new Mock<ITwitchApi>();
        twitch
            .Setup(c =>
                c.GetFullUserByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                twitchId is null ? null : new TwitchUserRestModel { TwitchId = twitchId }
            );

        grpc.SetupGet(client => client.WaifuGacha).Returns(waifu.Object);
        grpc.SetupGet(client => client.Twitch).Returns(twitch.Object);

        return grpc;
    }

    [Fact]
    public async Task FumoInventory_ShowsCountsAndTheList()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new FumoInventoryCommand(BuildGrpcClient(Inventory(), "5").Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["displayName"] = "player" },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        Assert.Contains("собрано 2/40 фумо", result.Text);
        Assert.Contains("Фумо Хина ×3", result.Text);
        Assert.Contains("Фумо Бэби ×1", result.Text);
    }

    [Fact]
    public async Task MikuInventory_UsesItsOwnUnit()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new MikuInventoryCommand(BuildGrpcClient(Inventory(), "5").Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["displayName"] = "player" },
            cancellationToken: ct
        );

        Assert.Contains("собрано 2/40 модулей", result.Text);
    }

    /// <summary>
    /// Адаптеры передают логин с ведущим <c>@</c>, а справочник ищет по чистому
    /// логину: без обрезки пользователь с никнейм вида <c>@player</c> всегда
    /// считался бы не найденным.
    /// </summary>
    [Fact]
    public async Task FumoInventory_StripsTheAtSignFromTheLogin()
    {
        var ct = TestContext.Current.CancellationToken;
        var grpc = BuildGrpcClient(Inventory(), "5");
        var twitch = new Mock<ITwitchApi>();
        twitch
            .Setup(c =>
                c.GetFullUserByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new TwitchUserRestModel { TwitchId = "5" });
        grpc.SetupGet(client => client.Twitch).Returns(twitch.Object);
        var command = new FumoInventoryCommand(grpc.Object);

        await command.ExecuteAsync(
            new Dictionary<string, object> { ["displayName"] = "@player" },
            cancellationToken: ct
        );

        twitch.Verify(
            c => c.GetFullUserByLoginAsync("player", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task FumoInventory_ReportsUnknownUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new FumoInventoryCommand(BuildGrpcClient(Inventory(), null).Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["displayName"] = "ghost" },
            cancellationToken: ct
        );

        Assert.False(result.Success);
        Assert.Contains("ghost", result.Text);
    }

    /// <summary>
    /// Пустой инвентарь — это результат, а не ошибка: игрок существует, просто
    /// ещё ничего не собрал.
    /// </summary>
    [Fact]
    public async Task FumoInventory_ReportsEmptyCollectionAsSuccess()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new FumoInventoryCommand(
            BuildGrpcClient(new CollectionInventory(0, 40, []), "5").Object
        );

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["displayName"] = "player" },
            cancellationToken: ct
        );

        Assert.True(result.Success);
        Assert.Contains("собрано 0/40 фумо", result.Text);
        Assert.DoesNotContain("Список", result.Text);
    }

    [Fact]
    public async Task FumoInventory_ReportsUnavailableService()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new FumoInventoryCommand(BuildGrpcClient(null, "5").Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["displayName"] = "player" },
            cancellationToken: ct
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.TargetUnreachable, result.ErrorCode);
    }

    [Fact]
    public async Task FumoInventory_Fails_WhenNoUserIsGiven()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new FumoInventoryCommand(BuildGrpcClient(Inventory(), null).Object);

        var result = await command.ExecuteAsync(
            new Dictionary<string, object>(),
            cancellationToken: ct
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.BadArguments, result.ErrorCode);
    }

    /// <summary>
    /// Длинная коллекция обрезается: сообщение чата Twitch ограничено по
    /// длине, и полный список не отправился бы вовсе.
    /// </summary>
    [Fact]
    public async Task FumoInventory_TruncatesALongList()
    {
        var ct = TestContext.Current.CancellationToken;
        var items = Enumerable
            .Range(0, 60)
            .Select(i => new CollectionItem($"Позиция-{i}", i + 1))
            .ToArray();
        var command = new FumoInventoryCommand(
            BuildGrpcClient(new CollectionInventory(items.Length, 200, items), "5").Object
        );

        var result = await command.ExecuteAsync(
            new Dictionary<string, object> { ["displayName"] = "player" },
            cancellationToken: ct
        );

        Assert.Contains("…", result.Text);
        Assert.True(
            result.Text.Length < 450 + 200,
            "Сообщение не должно быть длиннее предела списка плюс шапка"
        );
    }
}
