using MARS.Shared.Models;
using MARS.WaifuGacha.Configuration;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Призы вайфу для оверлея.
///
/// Список собирается постранично из всех вайфу. Проверяется, что в призы попадают
/// все записи и что картинка ведёт на сайт: без этого в оверлее были бы пустые
/// плитки.
/// </summary>
public class WaifuPrizesServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();

    /// <summary>
    /// Сайт из конфигурации не должен давать две косые черты в конце: картинка была бы
    /// не найдена.
    /// </summary>
    [Fact]
    public async Task ImageUrlIsAbsolute()
    {
        await SeedAsync("shiki-1", "Аяка", "/images/aya.png");
        await SeedAsync("shiki-2", "Мику", "images/miku.png");

        var result = await Create("https://shikimori.tk").GetWaifuPrizesAsync();

        Assert.True(result.Success);
        Assert.Equal(
            ["https://shikimori.tk/images/aya.png", "https://shikimori.tk/images/miku.png"],
            result.Result!.Select(prize => prize.Image).Order()
        );
    }

    /// <summary>
    /// Лишняя черта в настройке сайта не дублируется в ссылке.
    /// </summary>
    [Fact]
    public async Task TrailingSlashIsNotDuplicated()
    {
        await SeedAsync("shiki-1", "Аяка", "/images/aya.png");

        var result = await Create("https://shikimori.tk/").GetWaifuPrizesAsync();

        Assert.Equal("https://shikimori.tk/images/aya.png", result.Result!.Single().Image);
    }

    [Fact]
    public async Task NameBecomesPrizeText()
    {
        await SeedAsync("shiki-1", "Аяка", "/images/aya.png");

        var result = await Create("https://shikimori.tk").GetWaifuPrizesAsync();

        var prize = result.Result!.Single();
        Assert.Equal("Аяка", prize.Text);
        Assert.Equal("shiki-1", prize.Id);
    }

    /// <summary>
    /// Пустая база даёт пустой список, а не ошибку: оверлей показывает «призов нет».
    /// </summary>
    [Fact]
    public async Task EmptyDatabaseYieldsEmptyList()
    {
        var result = await Create("https://shikimori.tk").GetWaifuPrizesAsync();

        Assert.True(result.Success);
        Assert.Empty(result.Result!);
    }

    /// <summary>
    /// Ошибка базы возвращается как отказ с текстом, а не бросается наружу: список
    /// призов — украшение, а не причина уронить сервис.
    /// </summary>
    [Fact]
    public async Task DatabaseFailureIsReportedAsFailure()
    {
        var service = new WaifuPrizesService(
            new FailingFactory(),
            Options.Create(new ShikimoriSiteOptions { ShikimoriSite = "https://shikimori.tk" }),
            NullLogger<WaifuPrizesService>.Instance
        );

        var result = await service.GetWaifuPrizesAsync();

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private WaifuPrizesService Create(string site) =>
        new(
            _factory,
            Options.Create(new ShikimoriSiteOptions { ShikimoriSite = site }),
            NullLogger<WaifuPrizesService>.Instance
        );

    private async Task SeedAsync(string shikiId, string name, string imageUrl)
    {
        await using var db = await _factory.CreateDbContextAsync(Token);
        db.Waifus.Add(
            new Waifu
            {
                ShikiId = shikiId,
                Name = name,
                ImageUrl = imageUrl,
            }
        );
        await db.SaveChangesAsync(Token);
    }

    private sealed class FailingFactory : IDbContextFactory<WaifuDbContext>
    {
        public WaifuDbContext CreateDbContext() =>
            throw new InvalidOperationException("база недоступна");

        public Task<WaifuDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("база недоступна");
    }
}
