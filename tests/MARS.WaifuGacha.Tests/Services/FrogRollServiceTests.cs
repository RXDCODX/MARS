using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Ролл лягушки: та же схема «наименее заказываемый», но без коллекции.
/// </summary>
public class FrogRollServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly FrogRollService _service;

    public FrogRollServiceTests() => _service = new(_factory);

    [Fact]
    public async Task LeastOrderedFrogIsRolled()
    {
        await SeedAsync((1, DateTime.MinValue), (2, DateTime.MinValue.AddDays(1)));

        var frog = await _service.RollTheFrog();

        Assert.Equal(1, frog!.Pid);
    }

    [Fact]
    public async Task RollIncreasesOrderCount()
    {
        await SeedAsync((1, DateTime.MinValue));

        await _service.RollTheFrog();

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        Assert.Equal(
            1,
            await db
                .Frogs.Where(f => f.Pid == 1)
                .Select(f => f.OrderCount)
                .FirstAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task NextRollGoesToAnotherFrog()
    {
        await SeedAsync((1, DateTime.MinValue), (2, DateTime.MinValue.AddDays(1)));

        await _service.RollTheFrog();
        var second = await _service.RollTheFrog();

        Assert.Equal(2, second!.Pid);
    }

    [Fact]
    public async Task EmptyTableYieldsNoRoll()
    {
        Assert.Null(await _service.RollTheFrog());
    }

    [Fact]
    public async Task PrizesAreSortedByPid()
    {
        await SeedAsync((2, DateTime.MinValue), (1, DateTime.MinValue.AddDays(1)));

        var prizes = await _service.GetFrogPrizesAsync();

        Assert.True(prizes.Success);
        Assert.Equal(2, prizes.Result!.Count);
        Assert.Equal("1", prizes.Result.First().Id);
    }

    [Fact]
    public async Task NoPrizesYieldsEmptyList()
    {
        var prizes = await _service.GetFrogPrizesAsync();

        Assert.True(prizes.Success);
        Assert.Empty(prizes.Result!);
    }

    private async Task SeedAsync(params (int Pid, DateTime LastOrder)[] frogs)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        foreach (var frog in frogs)
        {
            db.Frogs.Add(
                new Frog
                {
                    Pid = frog.Pid,
                    CommonName = $"Лягушка {frog.Pid}",
                    ScientificName = "Rana",
                    ThumbnailUrl = "https://example.org/frog.png",
                    LastOrder = frog.LastOrder,
                }
            );
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
