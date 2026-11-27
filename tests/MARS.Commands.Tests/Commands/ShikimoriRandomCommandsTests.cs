using MARS.Commands.Application.Services.Entitys;
using MARS.Commands.Application.Services.Entitys.Commands;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.WaifuGacha;
using MARS.Shared.Contracts.Clients;
using Moq;

namespace MARS.Commands.Tests.Commands;

public class ShikimoriRandomCommandsTests
{
    private static IMarsGrpcClient BuildClient(ShikimoriTitleRef? anime, ShikimoriTitleRef? manga)
    {
        var waifuGacha = new Mock<IWaifuGachaApi>();
        waifuGacha
            .Setup(c => c.GetRandomAnimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(anime);
        waifuGacha
            .Setup(c => c.GetRandomMangaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(manga);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.WaifuGacha).Returns(waifuGacha.Object);

        return grpc.Object;
    }

    private static ShikimoriTitleRef Title() =>
        new(21, "One Piece", "Ван-Пис", 1999, "https://shikimori.one/animes/21");

    [Fact]
    public async Task RandomAnime_PrefersTheRussianTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new RandomAnimeCommand(BuildClient(Title(), null));

        var result = await command.ExecuteAsync(
            new Dictionary<string, object>(),
            cancellationToken: ct
        );

        Assert.True(result.Success);
        Assert.Contains("Ван-Пис (1999 г.)", result.Text);
        Assert.Contains("https://shikimori.one/animes/21", result.Text);
    }

    [Fact]
    public async Task RandomManga_PrefersTheRussianTitle()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new RandomMangaCommand(BuildClient(null, Title()));

        var result = await command.ExecuteAsync(
            new Dictionary<string, object>(),
            cancellationToken: ct
        );

        Assert.True(result.Success);
        Assert.Contains("Ван-Пис (1999 г.)", result.Text);
    }

    /// <summary>
    /// Неполное название выводится вопросительным знаком, а не пустой строкой:
    /// год выхода у Shikimori заполнен не у всех произведений.
    /// </summary>
    [Theory]
    [InlineData(null, null, "?")]
    [InlineData("Berserk", null, "Berserk")]
    public async Task RandomAnime_FallsBackWhenTheSourceHasNoRussianName(
        string? russian,
        string? name,
        string expected
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var title = new ShikimoriTitleRef(1, name, russian, null, "https://shikimori.one/animes/1");
        var command = new RandomAnimeCommand(BuildClient(title, null));

        var result = await command.ExecuteAsync(
            new Dictionary<string, object>(),
            cancellationToken: ct
        );

        Assert.Contains($"{expected} (? г.)", result.Text);
    }

    [Fact]
    public async Task RandomAnime_ReportsUnavailableService()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new RandomAnimeCommand(BuildClient(null, null));

        var result = await command.ExecuteAsync(
            new Dictionary<string, object>(),
            cancellationToken: ct
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.TargetUnreachable, result.ErrorCode);
    }

    [Fact]
    public async Task RandomManga_ReportsUnavailableService()
    {
        var ct = TestContext.Current.CancellationToken;
        var command = new RandomMangaCommand(BuildClient(null, null));

        var result = await command.ExecuteAsync(
            new Dictionary<string, object>(),
            cancellationToken: ct
        );

        Assert.False(result.Success);
        Assert.Equal(CommandErrorCode.TargetUnreachable, result.ErrorCode);
    }
}
