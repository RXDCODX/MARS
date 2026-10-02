using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Коллекция фумо: что выпадало пользователю и сколько раз.
///
/// Проверяется гарантия. Игрок может выбить пять одинаковых фумо подряд или
/// собрать двадцать разных по одному — в обоих случаях выдаётся тот, которого
/// у него ещё нет, иначе коллекция перестала бы расти сама по себе.
/// </summary>
public class FumoCollectionServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly FumoCollectionService _service;

    public FumoCollectionServiceTests() => _service = new(_factory);

    [Fact]
    public async Task FirstRollOfItemIsNew()
    {
        await SeedFumosAsync(101);

        var stats = await _service.RecordRollAsync("123456789", 101);

        Assert.True(stats.IsNew);
        Assert.Equal(1, stats.ThisItemCount);
        Assert.Equal(1, stats.CollectedCount);
        Assert.False(stats.GuaranteeTriggered);
    }

    [Fact]
    public async Task SecondRollOfSameItemIsNotNew()
    {
        await SeedFumosAsync(101);

        await _service.RecordRollAsync("123456789", 101);
        var stats = await _service.RecordRollAsync("123456789", 101);

        Assert.False(stats.IsNew);
        Assert.Equal(2, stats.ThisItemCount);
    }

    /// <summary>
    /// На пятом одинаковом фумо выдаётся новый экземпляр: иначе пять роллов
    /// подряд не давали бы ничего, кроме того же предмета.
    /// </summary>
    [Fact]
    public async Task FifthIdenticalItemTriggersGuarantee()
    {
        await SeedFumosAsync(101, 102);

        for (var roll = 0; roll < 4; roll++)
        {
            await _service.RecordRollAsync("123456789", 101);
        }

        var stats = await _service.RecordRollAsync("123456789", 101);

        Assert.Equal(5, stats.ThisItemCount);
        Assert.True(stats.GuaranteeTriggered);
        Assert.Equal(102, stats.GuaranteedItemId);
    }

    /// <summary>
    /// Двадцатый разный предмет даёт гарантию на двадцать первый: так коллекция
    /// не застревает на «собрал двадцать — и всё».
    /// </summary>
    [Fact]
    public async Task TwentiethUniqueItemTriggersGuarantee()
    {
        await SeedFumosAsync([.. Enumerable.Range(1, 21)]);

        FumoCollectionStats? stats = null;
        for (var index = 1; index <= 20; index++)
        {
            stats = await _service.RecordRollAsync("123456789", index);
        }

        Assert.NotNull(stats);
        Assert.Equal(20, stats!.CollectedCount);
        Assert.True(stats.GuaranteeTriggered);
        Assert.Equal(21, stats.GuaranteedItemId);
    }

    [Fact]
    public async Task CollectionStatsAreReported()
    {
        await SeedFumosAsync(101, 102);
        await _service.RecordRollAsync("123456789", 101);

        var stats = await _service.GetUserFumoCollectionStatsAsync("123456789");

        Assert.Equal(1, stats.collected);
        Assert.Equal(2, stats.total);
    }

    [Fact]
    public async Task StatsOfUnknownUserAreZero()
    {
        var stats = await _service.GetUserFumoCollectionStatsAsync("123456789");

        Assert.Equal(0, stats.collected);
        Assert.Equal(0, stats.total);
    }

    /// <summary>
    /// Инвентарь отсортирован по количеству убыванию: так он читается как «что
    /// выпадало чаще всего», а не по внутреннему порядку строк.
    /// </summary>
    [Fact]
    public async Task InventoryIsSortedByCountDescending()
    {
        await SeedFumosAsync(101, 102);
        await _service.RecordRollAsync("123456789", 101);
        await _service.RecordRollAsync("123456789", 102);
        await _service.RecordRollAsync("123456789", 102);

        var inventory = await _service.GetInventoryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.True(inventory.Success);
        Assert.Equal(2, inventory.Result!.Collected);
        Assert.Equal(2, inventory.Result.Total);
        Assert.Equal("Фумо 102", inventory.Result.Items.First().Name);
    }

    [Fact]
    public async Task EmptyInventoryIsNotAnError()
    {
        var inventory = await _service.GetInventoryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.True(inventory.Success);
        Assert.Equal(0, inventory.Result!.Collected);
    }

    [Fact]
    public async Task InventoryWithoutUserIdFails()
    {
        var inventory = await _service.GetInventoryAsync(
            "  ",
            TestContext.Current.CancellationToken
        );

        Assert.False(inventory.Success);
    }

    private async Task SeedFumosAsync(params int[] mfcIds)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        foreach (var mfcId in mfcIds)
        {
            db.Fumos.Add(
                new Fumo
                {
                    MfcId = mfcId,
                    Name = $"Фумо {mfcId}",
                    Character = "Character",
                    ThumbnailUrl = "https://example.org/fumo.png",
                    LastOrder = DateTime.MinValue.AddDays(mfcId),
                }
            );
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
