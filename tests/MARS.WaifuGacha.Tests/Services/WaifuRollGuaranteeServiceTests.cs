using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Гарантия выпадения VIP: после двухсот роллов он выпадает обязательно.
///
/// Проверяется именно гарантия — её иначе не проверить: случайность с шансом 1.5%
/// в тесте не воспроизводится, а вот счётчик роллов, который копится hundreds
/// раз, копится детерминированно.
/// </summary>
public class WaifuRollGuaranteeServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly WaifuRollGuaranteeService _service = new(
        new WaifuTestDbContextFactory(),
        NullLogger<WaifuRollGuaranteeService>.Instance
    );

    public WaifuRollGuaranteeServiceTests() => Service = Create(_factory);

    private WaifuRollGuaranteeService Service { get; }

    [Fact]
    public async Task FirstRollCreatesGuarantee()
    {
        await Service.IncrementRollCountAsync("123456789");

        var guarantee = await GuaranteeAsync("123456789");
        Assert.NotNull(guarantee);
        Assert.Equal(1, guarantee!.RollCount);
    }

    [Fact]
    public async Task NextRollIncrementsCounter()
    {
        await SeedAsync("123456789", 7);

        var result = await Service.IncrementRollCountAsync("123456789");

        Assert.True(result.Success);
        Assert.Equal(8, (await GuaranteeAsync("123456789"))!.RollCount);
    }

    /// <summary>
    /// На двухсотом ролле VIP выпадает вне зависимости от случайности, и запись
    /// гаранта удаляется: иначе тот же выигрыш выпадал бы снова и снова.
    /// </summary>
    [Fact]
    public async Task VipIsGuaranteedAfterTwoHundredRolls()
    {
        await SeedAsync("123456789", 200);

        var result = await Service.CheckVipDropAsync("123456789");

        Assert.True(result.Success);
        Assert.True(result.Result!.IsVipDropped);
        Assert.Equal("Гарант", result.Result.DropReason);
        Assert.Null(await GuaranteeAsync("123456789"));
    }

    [Fact]
    public async Task RollCountIsReportedBeforeTwoHundred()
    {
        await SeedAsync("123456789", 5);

        var result = await Service.CheckVipDropAsync("123456789");

        Assert.True(result.Success);
        Assert.Equal(5, result.Result!.RollCount);
    }

    [Fact]
    public async Task UnknownUserHasNoGuarantee()
    {
        var result = await Service.GetGuaranteeInfoAsync("123456789");

        Assert.True(result.Success);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task CounterIsReset()
    {
        await SeedAsync("123456789", 9);

        var result = await Service.ResetRollCountAsync("123456789");

        Assert.True(result.Success);
        Assert.Equal(0, (await GuaranteeAsync("123456789"))!.RollCount);
    }

    /// <summary>
    /// Сброс несуществующего гаранта — это ошибка, а не успех: команда сброса
    /// сообщала бы «готово» пользователю, у которого счётчика не было.
    /// </summary>
    [Fact]
    public async Task ResetOfUnknownUserFails()
    {
        var result = await Service.ResetRollCountAsync("123456789");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GuaranteeIsDeleted()
    {
        await SeedAsync("123456789", 3);

        var result = await Service.DeleteGuaranteeAsync("123456789");

        Assert.True(result.Success);
        Assert.Null(await GuaranteeAsync("123456789"));
    }

    [Fact]
    public async Task DeletionOfUnknownGuaranteeFails()
    {
        var result = await Service.DeleteGuaranteeAsync("123456789");

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankIdIsRejectedByEveryOperation(string twitchId)
    {
        Assert.False((await Service.IncrementRollCountAsync(twitchId)).Success);
        Assert.False((await Service.CheckVipDropAsync(twitchId)).Success);
        Assert.False((await Service.GetGuaranteeInfoAsync(twitchId)).Success);
        Assert.False((await Service.ResetRollCountAsync(twitchId)).Success);
        Assert.False((await Service.DeleteGuaranteeAsync(twitchId)).Success);
    }

    private static WaifuRollGuaranteeService Create(WaifuTestDbContextFactory factory) =>
        new(factory, NullLogger<WaifuRollGuaranteeService>.Instance);

    private async Task<WaifuRollGuarantee?> GuaranteeAsync(string twitchId)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db.WaifuRollGuarantees.FirstOrDefaultAsync(
            guarantee => guarantee.TwitchId == twitchId,
            TestContext.Current.CancellationToken
        );
    }

    private async Task SeedAsync(string twitchId, int rollCount)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.WaifuRollGuarantees.Add(
            new WaifuRollGuarantee { TwitchId = twitchId, RollCount = rollCount }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
