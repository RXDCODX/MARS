using MARS.Shared.Clients;
using MARS.Shared.Concurrency;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Models;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Ролл вайфу и свадьба.
///
/// Кулдаун — главное, что здесь проверяется: без него ролл работал бы на
/// каждом сообщении, и случайная вайфу выпадала бы двадцать раз в минуту.
/// Форсированный ролл (`forcePass`) обходит кулдаун — им пользуется Telegram,
/// и именно поэтому он проверяется отдельно.
/// </summary>
public class WaifuRollServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly WaifuRollService _service;

    public WaifuRollServiceTests()
    {
        var helper = new WaifuRollEnsurenceService(
            NullLogger<WaifuRollEnsurenceService>.Instance,
            Mock.Of<IShikimoriApiClient>(),
            _factory
        );

        _service = new WaifuRollService(
            _factory,
            NullLogger<WaifuRollService>.Instance,
            helper,
            new KeyedAsyncLock(),
            new RollCooldownConfigurationService(
                _factory,
                NullLogger<RollCooldownConfigurationService>.Instance
            )
        );
    }

    [Fact]
    public async Task FirstRollReturnsWaifu()
    {
        await SeedWaifusAsync(("waifu-1", DateTime.MinValue));

        var waifu = await _service.RollTheWaifu("123456789");

        Assert.Equal("waifu-1", waifu!.ShikiId);
    }

    /// <summary>
    /// Второй ролл в пределах кулдауна возвращает null, а не вайфу: иначе
    /// команда отвечала бы вайфу на каждое сообщение в чате.
    /// </summary>
    [Fact]
    public async Task RollInsideCooldownYieldsNothing()
    {
        await SeedWaifusAsync(("waifu-1", DateTime.MinValue));
        await _service.RollTheWaifu("123456789");

        var second = await _service.RollTheWaifu("123456789");

        Assert.Null(second);
    }

    [Fact]
    public async Task ForcedRollIgnoresCooldown()
    {
        await SeedWaifusAsync(("waifu-1", DateTime.MinValue));
        await _service.RollTheWaifu("123456789");

        var forced = await _service.RollTheWaifu("123456789", displayName: "Pyro", forcePass: true);

        Assert.NotNull(forced);
    }

    [Fact]
    public async Task RollWithoutWaifusYieldsNothing()
    {
        Assert.Null(await _service.RollTheWaifu("123456789"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankIdIsNotRolled(string id)
    {
        Assert.Null(await _service.RollTheWaifu(id));
    }

    /// <summary>
    /// Ролл не считается в счётчиках принудительно: иначе десять роллов из
    /// Telegram переписывали бы статистику чата.
    /// </summary>
    [Fact]
    public async Task ForcedRollKeepsOrderCount()
    {
        await SeedWaifusAsync(("waifu-1", DateTime.MinValue));
        await _service.RollTheWaifu("123456789");

        await _service.RollTheWaifu("123456789", displayName: "Pyro", forcePass: true);

        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        Assert.Equal(
            1,
            await db
                .Waifus.Where(w => w.ShikiId == "waifu-1")
                .Select(w => w.OrderCount)
                .FirstAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task MergeMakesBothPrivate()
    {
        await SeedWaifusAsync(("waifu-1", DateTime.MinValue));
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var host = new Husband
        {
            TwitchId = "123456789",
            HusbandGreetings = new HusbandAutoHello { HusbandId = "123456789" },
            HusbandCoolDown = new HusbandCoolDown { HusbandId = "123456789" },
        };
        db.Husbands.Add(host);
        var waifu = await db.Waifus.FirstAsync(TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var merged = await _service.MergeTheWaifu(host, waifu);

        Assert.True(merged);
        Assert.True(waifu.IsPrivated);
        Assert.True(host.IsPrivated);
        Assert.Equal("waifu-1", host.WaifuBrideId);
    }

    [Fact]
    public async Task MergeOfMissingEntitiesIsNotMerged()
    {
        Assert.False(await _service.MergeTheWaifu(null, null));
    }

    [Fact]
    public async Task UnmergeMakesBothFree()
    {
        await SeedWaifusAsync(("waifu-1", DateTime.MinValue));
        await using (
            var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            db.Husbands.Add(
                new Husband
                {
                    TwitchId = "123456789",
                    IsPrivated = true,
                    WaifuBrideId = "waifu-1",
                    WhenPrivated = DateTime.UtcNow,
                    HusbandGreetings = new HusbandAutoHello { HusbandId = "123456789" },
                    HusbandCoolDown = new HusbandCoolDown { HusbandId = "123456789" },
                }
            );
            var waifu = await db.Waifus.FirstAsync(TestContext.Current.CancellationToken);
            waifu.IsPrivated = true;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (
            var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            var host = await db.Husbands.FirstAsync(TestContext.Current.CancellationToken);
            var waifu = await db.Waifus.FirstAsync(TestContext.Current.CancellationToken);

            var unmerged = await _service.MergeTheWaifu(host, waifu, makeprivate: false);

            Assert.True(unmerged);
            Assert.False(waifu.IsPrivated);
            Assert.False(host.IsPrivated);
        }
    }

    [Fact]
    public async Task NewWaifuIsAdded()
    {
        var result = await _service.AddNewWaifu(
            new ShikimoriCharacterRef(
                1,
                "Аква",
                "Аква",
                null,
                "https://example.org/a.png",
                "/a.png",
                "Аниме",
                "Манга"
            )
        );

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Equal("1", result.Result!.Waifu!.ShikiId);
        Assert.Equal("Аква", result.Result.Waifu!.Name);
    }

    /// <summary>
    /// Русское имя предпочитается оригинальному: в чате рядом с русскими никами
    /// латиница читалась бы как опечатка.
    /// </summary>
    [Fact]
    public async Task NewWaifuPrefersRussianName()
    {
        var result = await _service.AddNewWaifu(
            new ShikimoriCharacterRef(
                2,
                "Akane",
                "Аканэ",
                null,
                "https://example.org/a.png",
                "/a.png",
                null,
                null
            )
        );

        Assert.NotNull(result.Result);
        Assert.Equal("Аканэ", result.Result!.Waifu!.Name);
    }

    [Fact]
    public async Task DuplicateWaifuIsRejected()
    {
        await SeedWaifusAsync(("2", DateTime.MinValue));

        var result = await _service.AddNewWaifu(
            new ShikimoriCharacterRef(
                2,
                "Akane",
                "Аканэ",
                null,
                "https://example.org/a.png",
                "/a.png",
                null,
                null
            )
        );

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AddingNothingFails()
    {
        var result = await _service.AddNewWaifu(null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TelegramRollOfUnknownHostFails()
    {
        var result = await _service.TelegramRollWaifu("никого-нет");

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task TelegramRollWithoutNameFails(string name)
    {
        var result = await _service.TelegramRollWaifu(name);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TelegramRollFindsHostByNickname()
    {
        await SeedWaifusAsync(("waifu-1", DateTime.MinValue));
        await using (
            var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            db.Husbands.Add(
                new Husband
                {
                    TwitchId = "pyro",
                    HusbandGreetings = new HusbandAutoHello { HusbandId = "pyro" },
                    HusbandCoolDown = new HusbandCoolDown { HusbandId = "pyro" },
                }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await _service.TelegramRollWaifu("pyr");

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Equal("pyro", result.Result!.Host!.TwitchId);
        Assert.NotNull(result.Result.Waifu);
    }

    [Fact]
    public async Task CooldownComesFromConfiguration()
    {
        var cooldown = await _service.GetWaifuRollCoolDownAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.FromMinutes(20), cooldown);
    }

    private async Task SeedWaifusAsync(params (string ShikiId, DateTime LastOrder)[] waifus)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        foreach (var waifu in waifus)
        {
            db.Waifus.Add(
                new Waifu
                {
                    ShikiId = waifu.ShikiId,
                    Name = $"Вайфу {waifu.ShikiId}",
                    ImageUrl = "https://example.org/waifu.png",
                    LastOrder = waifu.LastOrder,
                }
            );
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
