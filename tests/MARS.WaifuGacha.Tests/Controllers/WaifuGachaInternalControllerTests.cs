using MARS.Shared.Clients;
using MARS.Shared.Concurrency;
using MARS.Shared.Models;
using MARS.WaifuGacha.Controllers;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WaifuDbContext = MARS.WaifuGacha.Data.WaifuDbContext;

namespace MARS.WaifuGacha.Tests.Controllers;

/// <summary>
/// Внутренние межсервисные эндпоинты WaifuGacha.
///
/// Это закрытие зависимостей MARS.TwitchCore: имя супруга для русской рулетки и
/// текст авто-приветствия. Проверяется конверт ответа — сервисы отдают
/// <c>OperationResult</c>, и контроллер обязан отдать его вызывающему, а не
/// ронять вызов.
/// </summary>
public class WaifuGachaInternalControllerTests : IDisposable
{
    /// <summary>
    /// SQLite, а не InMemory: переключение авто-приветствия пишет через
    /// <c>ExecuteUpdateAsync</c>, и на InMemory такой запрос падает — тест прошёл
    /// бы, не проверив, что состояние действительно переключилось.
    /// </summary>
    private readonly WaifuSqliteTestDbContextFactory _factory = new();
    private readonly Mock<IShikimoriApiClient> _shikimori = new();
    private readonly WaifuGachaInternalController _controller;

    public WaifuGachaInternalControllerTests()
    {
        var anniversary = new WeddingAnniversaryService(
            _factory,
            NullLogger<WeddingAnniversaryService>.Instance
        );

        _controller = new WaifuGachaInternalController(
            new AutoHelloService(
                _factory,
                anniversary,
                new KeyedAsyncLock(),
                NullLogger<AutoHelloService>.Instance
            ),
            new FumoCollectionService(_factory),
            new MikuCollectionService(_factory),
            _shikimori.Object,
            _factory,
            NullLogger<WaifuGachaInternalController>.Instance
        )
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task WaifuNameIsReturned()
    {
        await SeedMarriedAsync(waifuName: "Аква");

        var response = await _controller.GetWaifuName(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.True(Envelope(response).Success);
        Assert.Equal("Аква", Envelope(response).Result);
    }

    /// <summary>
    /// Неприватизированный пользователь возвращает null, а не ошибку: русская
    /// рулетка спрашивает имя у всех, и «ошибка» превратилась бы в сообщение о
    /// сбое там, где просто нечего показывать.
    /// </summary>
    [Fact]
    public async Task WaifuNameOfUnknownUserIsNull()
    {
        var response = await _controller.GetWaifuName(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.True(Envelope(response).Success);
        Assert.Null(Envelope(response).Result);
    }

    [Fact]
    public async Task WaifuNameRejectsBlankId()
    {
        var response = await _controller.GetWaifuName("   ", TestContext.Current.CancellationToken);

        Assert.False(Envelope(response).Success);
    }

    [Fact]
    public async Task AutoHelloIsReturnedForMarriedUser()
    {
        await SeedMarriedAsync(waifuName: "Аква");

        var response = await _controller.GetAutoHello(
            "123456789",
            new AutoHelloRequest { DisplayName = "Pyro" },
            TestContext.Current.CancellationToken
        );

        Assert.True(Envelope(response).Success);
    }

    [Fact]
    public async Task AutoHelloRejectsMissingArguments()
    {
        var response = await _controller.GetAutoHello(
            "  ",
            new AutoHelloRequest(),
            TestContext.Current.CancellationToken
        );

        Assert.False(Envelope(response).Success);
    }

    /// <summary>
    /// Переключение возвращает новое состояние и действительно пишет его в базу:
    /// команда <c>!autohello</c> отвечает пользователю именно этим значением.
    /// </summary>
    [Fact]
    public async Task AutoHelloIsToggled()
    {
        await SeedMarriedAsync(waifuName: "Аква");

        var response = await _controller.ToggleAutoHello(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.True(Envelope(response).Success);
        Assert.True(Envelope(response).Result);
        Assert.True(await IsAutoHelloEnabledAsync());
    }

    [Fact]
    public async Task AutoHelloToggleRejectsBlankId()
    {
        var response = await _controller.ToggleAutoHello(
            " ",
            TestContext.Current.CancellationToken
        );

        Assert.False(Envelope(response).Success);
    }

    [Fact]
    public async Task FumoInventoryIsReturned()
    {
        var response = await _controller.GetFumoInventory("123456789");

        Assert.True(Envelope(response).Success);
    }

    [Fact]
    public async Task MikuInventoryIsReturned()
    {
        var response = await _controller.GetMikuInventory("123456789");

        Assert.True(Envelope(response).Success);
    }

    [Fact]
    public async Task RandomAnimeIsReturned()
    {
        _shikimori
            .Setup(instance => instance.GetRandomAnimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShikimoriTitleRef(1, "Аниме", "https://example.org", null, "Аниме"));

        var response = await _controller.GetRandomAnime(TestContext.Current.CancellationToken);

        Assert.True(Envelope(response).Success);
    }

    [Fact]
    public async Task MissingRandomAnimeIsReportedAsError()
    {
        _shikimori
            .Setup(instance => instance.GetRandomAnimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShikimoriTitleRef?)null);

        var response = await _controller.GetRandomAnime(TestContext.Current.CancellationToken);

        Assert.False(Envelope(response).Success);
    }

    [Fact]
    public async Task RandomMangaIsReturned()
    {
        _shikimori
            .Setup(instance => instance.GetRandomMangaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShikimoriTitleRef(2, "Манга", "https://example.org", null, "Манга"));

        var response = await _controller.GetRandomManga(TestContext.Current.CancellationToken);

        Assert.True(Envelope(response).Success);
    }

    [Fact]
    public async Task MissingRandomMangaIsReportedAsError()
    {
        _shikimori
            .Setup(instance => instance.GetRandomMangaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShikimoriTitleRef?)null);

        var response = await _controller.GetRandomManga(TestContext.Current.CancellationToken);

        Assert.False(Envelope(response).Success);
    }

    private static OperationResult<T> Envelope<T>(ActionResult<OperationResult<T>> response) =>
        Assert.IsType<OkObjectResult>(response.Result).Value is OperationResult<T> envelope
            ? envelope
            : throw new InvalidOperationException("контроллер вернул не OkObjectResult");

    private async Task<bool> IsAutoHelloEnabledAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db
            .Husbands.Select(h => h.IsAutoHelloEnabled)
            .FirstAsync(TestContext.Current.CancellationToken);
    }

    private async Task SeedMarriedAsync(string waifuName)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.Waifus.Add(
            new Waifu
            {
                ShikiId = "waifu-1",
                Name = waifuName,
                ImageUrl = "https://example.org/waifu.png",
            }
        );
        db.Husbands.Add(
            new Husband
            {
                TwitchId = "123456789",
                WhenOrdered = DateTime.Now,
                WhenPrivated = DateTime.Now,
                IsPrivated = true,
                WaifuBrideId = "waifu-1",
                HusbandGreetings = new HusbandAutoHello { HusbandId = "123456789" },
                HusbandCoolDown = new HusbandCoolDown { HusbandId = "123456789" },
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
