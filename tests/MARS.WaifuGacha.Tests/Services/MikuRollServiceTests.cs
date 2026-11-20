using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Services;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Ролл мику-модуля: выпадает наименее заказываемый.
/// </summary>
public class MikuRollServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly MikuRollService _service;

    public MikuRollServiceTests()
    {
        using (var cleanup = _factory.CreateDbContext())
        {
            cleanup.Waifus.RemoveRange(cleanup.Waifus);
            cleanup.Fumos.RemoveRange(cleanup.Fumos);
            cleanup.MikuModules.RemoveRange(cleanup.MikuModules);
            cleanup.MikuMondayTracks.RemoveRange(cleanup.MikuMondayTracks);
            cleanup.MikuMondayActivations.RemoveRange(cleanup.MikuMondayActivations);
            cleanup.Frogs.RemoveRange(cleanup.Frogs);
            cleanup.Husbands.RemoveRange(cleanup.Husbands);
            cleanup.WaifuRollAudios.RemoveRange(cleanup.WaifuRollAudios);
            cleanup.UserFumoCollections.RemoveRange(cleanup.UserFumoCollections);
            cleanup.UserMikuCollections.RemoveRange(cleanup.UserMikuCollections);
            cleanup.WaifuRollGuarantees.RemoveRange(cleanup.WaifuRollGuarantees);
            cleanup.AutoHelloMessages.RemoveRange(cleanup.AutoHelloMessages);
            cleanup.RollCooldowns.RemoveRange(cleanup.RollCooldowns);
            cleanup.HusbandCoolDowns.RemoveRange(cleanup.HusbandCoolDowns);
            cleanup.HusbandGreetings.RemoveRange(cleanup.HusbandGreetings);
            cleanup.SaveChanges();
        }

        _service = new(_factory);
    }

    [Fact]
    public async Task LeastOrderedModuleIsRolled()
    {
        await SeedAsync((101, DateTime.MinValue), (102, DateTime.MinValue.AddDays(1)));

        var module = await _service.RollTheMiku();

        Assert.Equal(101, module!.PageId);
    }

    [Fact]
    public async Task RollIncreasesOrderCount()
    {
        await SeedAsync((101, DateTime.MinValue));

        await _service.RollTheMiku();

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        Assert.Equal(
            1,
            await db
                .MikuModules.Where(m => m.PageId == 101)
                .Select(m => m.OrderCount)
                .FirstAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task NextRollGoesToAnotherModule()
    {
        await SeedAsync((101, DateTime.MinValue), (102, DateTime.MinValue.AddDays(1)));

        await _service.RollTheMiku();
        var second = await _service.RollTheMiku();

        Assert.Equal(102, second!.PageId);
    }

    [Fact]
    public async Task EmptyTableYieldsNoRoll()
    {
        Assert.Null(await _service.RollTheMiku());
    }

    [Fact]
    public async Task PrizesAreSortedByPageId()
    {
        await SeedAsync((102, DateTime.MinValue), (101, DateTime.MinValue.AddDays(1)));

        var prizes = await _service.GetMikuPrizesAsync();

        Assert.True(prizes.Success);
        Assert.Equal(2, prizes.Result!.Count);
        Assert.Equal("101", prizes.Result.First().Id);
    }

    [Fact]
    public async Task NoPrizesYieldsEmptyList()
    {
        var prizes = await _service.GetMikuPrizesAsync();

        Assert.True(prizes.Success);
        Assert.Empty(prizes.Result!);
    }

    private async Task SeedAsync(params (int PageId, DateTime LastOrder)[] modules)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        foreach (var module in modules)
        {
            db.MikuModules.Add(
                new MikuModule
                {
                    PageId = module.PageId,
                    Title = $"Модуль {module.PageId}",
                    ThumbnailUrl = "https://example.org/miku.png",
                    LastOrder = module.LastOrder,
                }
            );
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
