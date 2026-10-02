using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Ролл фумо.
///
/// Проверяется два свойства розыгрыша. Первое: выпадает тот, кто выпадал
/// реже всех, а не случайный — иначе один и тот же персонаж выпадал бы подряд.
/// Второе: порядок обновляется, и при следующем ролле приоритет уходит к другому.
/// </summary>
public class FumoRollServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly FumoRollService _service;

    public FumoRollServiceTests() => _service = new(_factory);

    [Fact]
    public async Task LeastOrderedFumoIsRolled()
    {
        await SeedAsync((101, DateTime.MinValue), (102, DateTime.MinValue.AddDays(1)));

        var fumo = await _service.RollTheFumo();

        Assert.Equal(101, fumo!.MfcId);
    }

    [Fact]
    public async Task RollIncreasesOrderCountAndLastOrder()
    {
        await SeedAsync((101, DateTime.MinValue));

        await _service.RollTheFumo();

        Assert.Equal(1, await OrderCountAsync(101));
        Assert.True(await LastOrderAsync(101) > DateTime.MinValue);
    }

    /// <summary>
    /// Второй ролл уходит к другому персонажу: без обновления времени оба
    /// остались бы «самыми редкими» и выпадали бы по кругу.
    /// </summary>
    [Fact]
    public async Task NextRollGoesToAnotherFumo()
    {
        await SeedAsync((101, DateTime.MinValue), (102, DateTime.MinValue.AddDays(1)));

        await _service.RollTheFumo();
        var second = await _service.RollTheFumo();

        Assert.Equal(102, second!.MfcId);
    }

    /// <summary>
    /// Пустая таблица фумо даёт null, а не исключение: команда ролла не должна
    /// падать, когда база ещё не наполнена.
    /// </summary>
    [Fact]
    public async Task EmptyTableYieldsNoRoll()
    {
        Assert.Null(await _service.RollTheFumo());
    }

    /// <summary>
    /// Имя персонажа транслитерируется в кириллицу: в чате латиница рядом с
    /// русскими никами выглядит опечаткой.
    /// </summary>
    [Fact]
    public async Task CharacterIsTransliteratedOnRoll()
    {
        await SeedAsync((101, DateTime.MinValue), character: "Reimu Hakurei", translit: null);

        var fumo = await _service.RollTheFumo();

        Assert.Equal("реиму хакуреи", fumo!.CharacterTranslit);
    }

    [Fact]
    public async Task PrizesAreReturned()
    {
        await SeedAsync((102, DateTime.MinValue), (101, DateTime.MinValue.AddDays(1)));

        var prizes = await _service.GetFumoPrizesAsync();

        Assert.True(prizes.Success);
        Assert.Equal(2, prizes.Result!.Count);
    }

    /// <summary>
    /// Список призов отсортирован по id, а не по порядку выпадения: иначе список
    /// в чате прыгал бы после каждого ролла.
    /// </summary>
    [Fact]
    public async Task PrizesAreSortedById()
    {
        await SeedAsync((102, DateTime.MinValue), (101, DateTime.MinValue.AddDays(1)));

        var prizes = await _service.GetFumoPrizesAsync();

        Assert.Equal(["101", "102"], prizes.Result!.Select(prize => prize.Id));
    }

    [Fact]
    public async Task NoPrizesYieldsEmptyList()
    {
        var prizes = await _service.GetFumoPrizesAsync();

        Assert.True(prizes.Success);
        Assert.Empty(prizes.Result!);
    }

    private async Task<int> OrderCountAsync(int mfcId)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db
            .Fumos.Where(f => f.MfcId == mfcId)
            .Select(f => f.OrderCount)
            .FirstAsync(TestContext.Current.CancellationToken);
    }

    private async Task<DateTime> LastOrderAsync(int mfcId)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db
            .Fumos.Where(f => f.MfcId == mfcId)
            .Select(f => f.LastOrder)
            .FirstAsync(TestContext.Current.CancellationToken);
    }

    private async Task SeedAsync(params (int MfcId, DateTime LastOrder)[] fumos) =>
        await SeedCoreAsync(
            fumos.Select(fumo => (fumo.MfcId, fumo.LastOrder, "Character", (string?)null)).ToArray()
        );

    private async Task SeedAsync(
        (int MfcId, DateTime LastOrder) single,
        string character,
        string? translit
    ) => await SeedCoreAsync([(single.MfcId, single.LastOrder, character, translit)]);

    private async Task SeedCoreAsync(
        (int MfcId, DateTime LastOrder, string Character, string? Translit)[] rows
    )
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        foreach (var row in rows)
        {
            db.Fumos.Add(
                new Fumo
                {
                    MfcId = row.MfcId,
                    Name = $"Фумо {row.MfcId}",
                    Character = row.Character,
                    CharacterTranslit = row.Translit,
                    ThumbnailUrl = "https://example.org/fumo.png",
                    LastOrder = row.LastOrder,
                }
            );
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
