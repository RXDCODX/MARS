using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.WeddingAnniversary;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Годовщины свадьбы.
///
/// Проверяется, что поздравляется ближайшая будущая годовщина и ровно один раз:
/// иначе стрим каждую неделю получал бы одно и то же поздравление, а пропущенные
/// месяцы поздравлялись бы задним числом.
///
/// Посев обязателен вместе с <see cref="TwitchUser"/>: связь Husband → TwitchUser
/// объявлена через обязательный внешний ключ, поэтому запрос с Include делает
/// внутреннее соединение и молча теряет мужа без пользователя. Пустой результат
/// при заполненной таблице — симптом именно этого.
/// </summary>
public class WeddingAnniversaryServiceTests
{
    private readonly TwitchTestDbContextFactory _factory = new();
    private readonly WeddingAnniversaryService _service;

    /// <summary>
    /// Сервис и посев обязаны ходить в одну и ту же базу: фабрика создаёт своё
    /// хранилище на каждый вызов, поэтому общая нужна одна.
    /// </summary>
    public WeddingAnniversaryServiceTests() =>
        _service = new(_factory, NullLogger<WeddingAnniversaryService>.Instance);

    /// <summary>
    /// Свадьба месяц назад: зелёная годовщина уже прошла, поэтому берётся следующая —
    /// год. Иначе поздравление пришло бы в прошлом.
    /// </summary>
    [Fact]
    public async Task PassedAnniversaryIsSkipped()
    {
        await SeedAsync(weddingDate: DateTime.Now.AddMonths(-1));

        var anniversary = await _service.GetNearestAnniversaryAsync(Token);

        Assert.NotNull(anniversary);
        Assert.Equal(12, anniversary!.Months);
    }

    /// <summary>
    /// Свадьба сегодня — это зелёная свадьба, и поздравлять надо именно её.
    /// </summary>
    [Fact]
    public async Task WeddingTodayIsGreenAnniversary()
    {
        await SeedAsync(weddingDate: DateTime.Now);

        var anniversary = await _service.GetNearestAnniversaryAsync(Token);

        Assert.NotNull(anniversary);
        Assert.Equal(0, anniversary!.Months);
    }

    /// <summary>
    /// Неженатый пользователь не попадает в разбор: годовщины у него нет.
    /// </summary>
    [Fact]
    public async Task SingleUserIsIgnored()
    {
        await SeedAsync(weddingDate: DateTime.Now.AddMonths(-1), isPrivated: false);

        Assert.Null(await _service.GetNearestAnniversaryAsync(Token));
    }

    /// <summary>
    /// Годовщина, которую уже поздравили, не предлагается снова: месяц отмечается
    /// один раз.
    /// </summary>
    [Fact]
    public async Task CongratulatedAnniversaryIsNotOfferedAgain()
    {
        await SeedAsync(weddingDate: DateTime.Now.AddMonths(-1), lastMonths: 0);

        var anniversary = await _service.GetNearestAnniversaryAsync(Token);

        Assert.NotNull(anniversary);
        Assert.NotEqual(0, anniversary!.Months);
    }

    /// <summary>
    /// Из нескольких пар ближайшей считается та, чья годовщина наступит раньше.
    /// </summary>
    [Fact]
    public async Task NearestAnniversaryAcrossUsersIsTheClosestOne()
    {
        await SeedAsync(twitchId: "1", weddingDate: DateTime.Now.AddMonths(-1));
        await SeedAsync(twitchId: "2", weddingDate: DateTime.Now.AddMonths(-5));

        var anniversary = await _service.GetNearestAnniversaryAsync(Token);

        Assert.Equal("2", anniversary!.TwitchId);
    }

    /// <summary>
    /// Отправленная годовщина возвращается один раз и помечается: повторный вызов
    /// не должен слать то же поздравление заново.
    /// </summary>
    [Fact]
    public async Task UnsentAnniversaryIsReturnedOnce()
    {
        await SeedAsync(weddingDate: DateTime.Now.AddMonths(-1));

        var first = await _service.GetNextUnsentAnniversaryAsync("123456789", Token);
        var second = await _service.GetNextUnsentAnniversaryAsync("123456789", Token);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    /// <summary>
    /// Ещё не наступившая годовщина не отправляется — поздравлять заранее незачем.
    /// </summary>
    [Fact]
    public async Task FutureAnniversaryIsNotReturned()
    {
        await SeedAsync(weddingDate: DateTime.Now.AddMonths(1));

        Assert.Null(await _service.GetNextUnsentAnniversaryAsync("123456789", Token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyUserIdIsNotSearched(string? twitchId)
    {
        Assert.Null(await _service.GetNextUnsentAnniversaryAsync(twitchId!, Token));
    }

    [Fact]
    public async Task MarkedMonthIsPersisted()
    {
        await SeedAsync(weddingDate: DateTime.Now.AddMonths(-13));

        await _service.MarkAnniversaryAsSentAsync("123456789", 12, Token);

        await using var db = await _factory.CreateDbContextAsync(Token);
        var host = await db.Husbands.FindAsync(["123456789"], Token);
        Assert.Equal(12, host!.LastWeddingCongratulatedMonths);
    }

    /// <summary>
    /// Зелёная свадьба — год ещё не прошёл, поэтому срок в сообщении не упоминается.
    /// </summary>
    [Fact]
    public void GreenAnniversaryHasNoYears()
    {
        var message = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
            "Pyro",
            "Аяка",
            (0, "Зелёная свадьба")
        );

        Assert.Contains("@Pyro", message);
        Assert.Contains("Аяка", message);
        Assert.Contains("Зелёной свадьбой", message);
        Assert.DoesNotContain("лет", message);
    }

    /// <summary>
    /// В годовщину сроком в год сообщение называет срок и склоняет название.
    /// </summary>
    [Fact]
    public void AnniversaryWithYearMentionsYears()
    {
        var message = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
            "Pyro",
            "Аяка",
            (12, "Ситцевая свадьба")
        );

        Assert.Contains("1 лет", message);
        Assert.Contains("Ситцевой свадьбой", message);
    }

    /// <summary>
    /// Розовая свадьба — это 10 лет, а не 120 месяцев: срок печатается годами, и
    /// название в скобках склоняется отдельно.
    /// </summary>
    [Fact]
    public void TenYearsIsNotAMonthCount()
    {
        var message = WeddingAnniversaryService.BuildCongratulationMessageFromSpouse(
            "Pyro",
            "Аяка",
            (120, "Оловянная свадьба (розовая)")
        );

        Assert.Contains("10 лет", message);
        Assert.Contains("розовой", message);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task SeedAsync(
        string twitchId = "123456789",
        DateTime? weddingDate = null,
        bool isPrivated = true,
        int lastMonths = -1
    )
    {
        await using var db = await _factory.CreateDbContextAsync(Token);
        db.TwitchUsers.Add(
            new TwitchUser
            {
                TwitchId = twitchId,
                UserLogin = $"login-{twitchId}",
                DisplayName = $"Pyro-{twitchId}",
            }
        );
        db.Husbands.Add(
            new Husband
            {
                TwitchId = twitchId,
                IsPrivated = isPrivated,
                WhenPrivated = weddingDate,
                LastWeddingCongratulatedMonths = lastMonths,
            }
        );
        await db.SaveChangesAsync(Token);
    }
}
