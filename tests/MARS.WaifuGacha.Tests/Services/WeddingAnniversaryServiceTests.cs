using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Годовщины свадьбы супругов.
///
/// Проверяются две вещи, из-за которых сервис и написан. Первая — дата: годовщина
/// наступает в день, равный дню начала отношений, а не «через N месяцев от
/// сегодня». Вторая — склонение: «зелёная свадьба» в сообщении должно стать
/// «зелёной свадьбой», иначе поздравление выглядит как ошибка.
/// </summary>
public class WeddingAnniversaryServiceTests
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly WeddingAnniversaryService _service = new(
        new WaifuTestDbContextFactory(),
        NullLogger<WeddingAnniversaryService>.Instance
    );

    public WeddingAnniversaryServiceTests() =>
        _service = new(_factory, NullLogger<WeddingAnniversaryService>.Instance);

    [Fact]
    public async Task AnniversaryDueTodayIsReported()
    {
        await SeedAsync(monthsAgo: 12, isPrivated: true, lastCongratulated: 0);

        var result = await _service.GetNextUnsentAnniversaryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(result);
        Assert.Equal(12, result!.Value.Months);
    }

    /// <summary>
    /// День свадьбы (0 месяцев) — первая годовщина в списке, и она тоже
    /// поздравляется: иначе самая важная дата молча пропускалась бы.
    /// </summary>
    [Fact]
    public async Task WeddingDayItselfIsReported()
    {
        await SeedAsync(monthsAgo: 0, isPrivated: true);

        var result = await _service.GetNextUnsentAnniversaryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, result!.Value.Months);
    }

    /// <summary>
    /// Неприватизированный пользователь не поздравляется: показ годовщины —
    /// личное, и оно не должно появляться в общем чате.
    /// </summary>
    [Fact]
    public async Task PublicUserHasNoAnniversary()
    {
        await SeedAsync(monthsAgo: 12, isPrivated: false);

        var result = await _service.GetNextUnsentAnniversaryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task AlreadyCongratulatedAnniversaryIsNotRepeated()
    {
        await SeedAsync(monthsAgo: 24, isPrivated: true, lastCongratulated: 12);

        var result = await _service.GetNextUnsentAnniversaryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(24, result!.Value.Months);
    }

    /// <summary>
    /// Годовщина, которой ещё не наступило, не выдаётся за наступившую.
    /// </summary>
    [Fact]
    public async Task FutureAnniversaryIsNotReported()
    {
        await SeedAsync(monthsAgo: 0, isPrivated: true, lastCongratulated: 0);

        var result = await _service.GetNextUnsentAnniversaryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankIdYieldsNoAnniversary(string twitchId)
    {
        var result = await _service.GetNextUnsentAnniversaryAsync(
            twitchId,
            TestContext.Current.CancellationToken
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task UnknownUserHasNoAnniversary()
    {
        var result = await _service.GetNextUnsentAnniversaryAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task MarkingSentDoesNotThrow()
    {
        await SeedAsync(monthsAgo: 12, isPrivated: true);

        await _service.MarkAnniversaryAsSentAsync(
            "123456789",
            12,
            TestContext.Current.CancellationToken
        );
    }

    [Fact]
    public async Task MarkingSentForUnknownUserIsSafe()
    {
        await _service.MarkAnniversaryAsSentAsync(
            "123456789",
            12,
            TestContext.Current.CancellationToken
        );
    }

    /// <summary>
    /// Поздравление без годовщины в годах не упоминает лет: «поздравляет с
    /// зелёной свадьбой (0 лет)» звучало бы нелепо.
    /// </summary>
    [Fact]
    public void FirstAnniversaryMessageHasNoYears()
    {
        var message = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
            "Pyro",
            "Anna",
            (0, "Зелёная свадьба")
        );

        Assert.Contains("@Pyro", message);
        Assert.Contains("Anna", message);
        Assert.Contains("Зелёной свадьбой", message);
        Assert.DoesNotContain("лет", message);
    }

    [Fact]
    public void AnniversaryMessageDeclinesNameAndCountsYears()
    {
        var message = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
            "Pyro",
            "Anna",
            (12, "Ситцевая свадьба")
        );

        Assert.Contains("Ситцевой свадьбой", message);
        Assert.Contains("1 лет", message);
    }

    [Theory]
    [InlineData(24, "2 лет")]
    [InlineData(30, "2,5 лет")]
    public void YearsAreFormattedWithRuCulture(int months, string expected)
    {
        var message = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
            "Pyro",
            "Anna",
            (months, "Свадьба")
        );

        Assert.Contains(expected, message);
    }

    /// <summary>
    /// Название в скобках склоняется так же, как и без них: «Оловянная (розовая)
    /// свадьба» в сообщении читалась бы как ошибка.
    /// </summary>
    [Fact]
    public void NameInsideBracketsIsDeclined()
    {
        var message = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
            "Pyro",
            "Anna",
            (120, "Оловянная свадьба (розовая)")
        );

        Assert.Contains("Оловянной свадьбой (розовой)", message);
    }

    private async Task SeedAsync(int monthsAgo, bool isPrivated, int? lastCongratulated = null)
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.Husbands.Add(
            new Husband
            {
                TwitchId = "123456789",
                WhenOrdered = DateTime.Now.AddMonths(-monthsAgo),
                WhenPrivated = DateTime.Now.AddMonths(-monthsAgo),
                IsPrivated = isPrivated,
                LastWeddingCongratulatedMonths = lastCongratulated,
                HusbandGreetings = new HusbandAutoHello { HusbandId = "123456789" },
                HusbandCoolDown = new HusbandCoolDown { HusbandId = "123456789" },
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
