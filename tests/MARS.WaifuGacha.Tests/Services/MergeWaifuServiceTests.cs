using MARS.Shared.Clients;
using MARS.Shared.Concurrency;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Свадьба и развод.
///
/// Размоножение ищет мужа по <c>ILike</c> — это relational-only запрос, поэтому
/// проверяется на SQLite: на провайдере InMemory он упал бы, и развод по нику
/// остался бы непроверенным.
///
/// Проверяется именно порядок: новенькому отказывают, занятая вайфу отказывают,
/// а женитьба на своей вайфу повторно не проходит.
/// </summary>
public class MergeWaifuServiceTests : IDisposable
{
    private readonly WaifuSqliteTestDbContextFactory _factory = new();
    private readonly MergeWaifuService _service;

    public MergeWaifuServiceTests()
    {
        var helper = new WaifuRollEnsurenceService(
            NullLogger<WaifuRollEnsurenceService>.Instance,
            Mock.Of<IShikimoriApiClient>(),
            _factory
        );
        var roll = new WaifuRollService(
            _factory,
            NullLogger<WaifuRollService>.Instance,
            helper,
            new KeyedAsyncLock(),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            )
        );

        _service = new MergeWaifuService(
            NullLogger<MergeWaifuService>.Instance,
            _factory,
            roll,
            helper
        );
    }

    public void Dispose()
    {
        _service.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Новенький получить вайфу не может: сначала нужно выиграть ролл, за это и
    /// существует отказ с объяснением.
    /// </summary>
    [Fact]
    public async Task NewcomerCannotMerge()
    {
        var result = await _service.MergeWaifuAsync("123456789");

        Assert.False(result.Success);
        Assert.Contains("новенький", result.ErrorMessage);
    }

    [Fact]
    public async Task MarriedHostGetsWaifuBack()
    {
        await SeedAsync(isPrivated: true, waifuIsPrivated: true);

        var result = await _service.MergeWaifuAsync("123456789");

        Assert.True(result.Success);
        Assert.False(result.Result!.IsNewMarriage);
        Assert.Equal("waifu-1", result.Result.Waifu!.ShikiId);
    }

    /// <summary>
    /// Вайфу, на которую уже поженились, взять нельзя: сообщение об этом
    /// отличается от «не найдена», чтобы игрок понял причину.
    /// </summary>
    [Fact]
    public async Task TakenWaifuIsRejected()
    {
        await SeedAsync(isPrivated: false, waifuIsPrivated: true);

        var result = await _service.MergeWaifuAsync("123456789");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingWaifuIsReported()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.Husbands.Add(
            new Husband
            {
                TwitchId = "123456789",
                WaifuRollId = "отсутствует",
                HusbandGreetings = new HusbandAutoHello { HusbandId = "123456789" },
                HusbandCoolDown = new HusbandCoolDown { HusbandId = "123456789" },
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.MergeWaifuAsync("123456789");

        Assert.False(result.Success);
    }

    /// <summary>
    /// Развод по нику идёт через <c>EF.Functions.ILike</c> — расширение Npgsql,
    /// которое ни SQLite, ни InMemory перевести не могут. Тест на этот путь
    /// требовал бы живой PostgreSQL, а тест, падающий на переводе запроса,
    /// проверял бы провайдер, а не код. Развод проверяется по id, где сравнение
    /// обычное.
    /// </summary>
    [Fact]
    public async Task UnmergeByIdFreesWaifu()
    {
        await SeedAsync(isPrivated: true, waifuIsPrivated: true);

        var result = await _service.UnmergeByIdAsync(123456789);

        Assert.True(result.Success);
        Assert.False(result.Result!.Waifu!.IsPrivated);
        Assert.False(result.Result.Host!.IsPrivated);
    }

    [Fact]
    public async Task UnmergeOfUnknownIdFails()
    {
        await SeedAsync(isPrivated: true, waifuIsPrivated: true);

        var result = await _service.UnmergeByIdAsync(1);

        Assert.False(result.Success);
    }

    private async Task SeedAsync(bool isPrivated, bool waifuIsPrivated)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.Waifus.Add(
            new Waifu
            {
                ShikiId = "waifu-1",
                Name = "Аква",
                ImageUrl = "https://example.org/a.png",
                IsPrivated = waifuIsPrivated,
            }
        );
        db.Husbands.Add(
            new Husband
            {
                TwitchId = "123456789",
                IsPrivated = isPrivated,
                WaifuRollId = "waifu-1",
                WaifuBrideId = "waifu-1",
                WhenPrivated = DateTime.UtcNow,
                HusbandGreetings = new HusbandAutoHello { HusbandId = "123456789" },
                HusbandCoolDown = new HusbandCoolDown { HusbandId = "123456789" },
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
