using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Коллекция мику-модулей: правила те же, что у фумо, но ключ — id страницы.
///
/// Гарантия проверяется отдельно от счётчиков: без неё собирание коллекции
/// упиралось бы в случайность, и двадцать разных модулей подряд были бы
/// практически недостижимы.
/// </summary>
public class MikuCollectionServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly MikuCollectionService _service;

    public MikuCollectionServiceTests() => _service = new(_factory);

    [Fact]
    public async Task FirstRollOfModuleIsNew()
    {
        await SeedModulesAsync(101);

        var stats = await _service.RecordRollAsync("123456789", 101);

        Assert.True(stats.IsNew);
        Assert.Equal(1, stats.ThisModuleCount);
        Assert.Equal(1, stats.CollectedCount);
    }

    [Fact]
    public async Task SecondRollOfSameModuleIncrementsCounter()
    {
        await SeedModulesAsync(101);

        await _service.RecordRollAsync("123456789", 101);
        var stats = await _service.RecordRollAsync("123456789", 101);

        Assert.False(stats.IsNew);
        Assert.Equal(2, stats.ThisModuleCount);
    }

    [Fact]
    public async Task FifthIdenticalModuleTriggersGuarantee()
    {
        await SeedModulesAsync(101, 102);

        for (var roll = 0; roll < 4; roll++)
        {
            await _service.RecordRollAsync("123456789", 101);
        }

        var stats = await _service.RecordRollAsync("123456789", 101);

        Assert.True(stats.GuaranteeTriggered);
        Assert.Equal(102, stats.GuaranteedPageId);
    }

    [Fact]
    public async Task CollectionStatsAreReported()
    {
        await SeedModulesAsync(101, 102);
        await _service.RecordRollAsync("123456789", 101);

        var stats = await _service.GetUserCollectionStatsAsync("123456789");

        Assert.Equal(1, stats.collected);
        Assert.Equal(2, stats.total);
    }

    [Fact]
    public async Task StatsOfUnknownUserAreZero()
    {
        var stats = await _service.GetUserCollectionStatsAsync("123456789");

        Assert.Equal((0, 0), stats);
    }

    [Fact]
    public async Task InventoryIsReturned()
    {
        await SeedModulesAsync(101, 102);
        await _service.RecordRollAsync("123456789", 101);

        var inventory = await _service.GetInventoryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.True(inventory.Success);
        Assert.Equal(1, inventory.Result!.Collected);
        Assert.Equal(2, inventory.Result.Total);
    }

    [Fact]
    public async Task InventoryWithoutUserIdFails()
    {
        var inventory = await _service.GetInventoryAsync(
            " ",
            TestContext.Current.CancellationToken
        );

        Assert.False(inventory.Success);
    }

    private async Task SeedModulesAsync(params int[] pageIds)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        foreach (var pageId in pageIds)
        {
            db.MikuModules.Add(
                new MikuModule
                {
                    PageId = pageId,
                    Title = $"Модуль {pageId}",
                    ThumbnailUrl = "https://example.org/miku.png",
                    LastOrder = DateTime.MinValue.AddDays(pageId),
                }
            );
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
