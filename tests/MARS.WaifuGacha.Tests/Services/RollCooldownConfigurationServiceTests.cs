using MARS.Shared.Clients;
using MARS.Shared.Concurrency;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Кулдауны роллов из <c>waifu.RootState</c>.
///
/// Сервис нужен из-за исторической ошибки: ключи кулдаунов завёл MARS.Admin в
/// своей схеме, WaifuGacha их не видел и всегда возвращал зашитые 20 минут.
/// Проверяется и зашивка, и чтение значения из базы.
/// </summary>
public class RollCooldownConfigurationServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly RollCooldownConfigurationService _service = new(
        new WaifuTestDbContextFactory(),
        NullLogger<RollCooldownConfigurationService>.Instance
    );

    public RollCooldownConfigurationServiceTests() =>
        _service = new(_factory, NullLogger<RollCooldownConfigurationService>.Instance);

    [Fact]
    public async Task MissingConfigurationFallsBackToTwentyMinutes()
    {
        var cooldown = await _service.GetCooldownAsync(
            RootStateKeys.WaifuRollType,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.FromMinutes(20), cooldown);
    }

    [Fact]
    public async Task ConfiguredValueIsRead()
    {
        await SeedAsync(RootStateKeys.FumoRollCooldownMinutes, "45");

        var cooldown = await _service.GetCooldownAsync(
            RootStateKeys.FumoRollType,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.FromMinutes(45), cooldown);
    }

    /// <summary>
    /// Ноль и мусор в значении игнорируются: нулевой кулдаун превращал бы ролл
    /// в спам, а значение не из числа вообще не должно доходить до таймера.
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("не число")]
    [InlineData("")]
    public async Task UnusableValuesFallBackToDefault(string value)
    {
        await SeedAsync(RootStateKeys.MikuRollCooldownMinutes, value);

        var cooldown = await _service.GetCooldownAsync(
            RootStateKeys.MikuRollType,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.FromMinutes(20), cooldown);
    }

    /// <summary>
    /// Неизвестный тип ролла читает ключ вайфу: новая разновидность ролла не
    /// должна ломать существующий дефолт.
    /// </summary>
    [Fact]
    public async Task UnknownRollTypeUsesWaifuKey()
    {
        await SeedAsync(RootStateKeys.WaifuRollCooldownMinutes, "7");

        var cooldown = await _service.GetCooldownAsync(
            "что-то новое",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TimeSpan.FromMinutes(7), cooldown);
    }

    private async Task SeedAsync(string name, string value)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.RootState.Add(new RootState { Name = name, Value = value });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
