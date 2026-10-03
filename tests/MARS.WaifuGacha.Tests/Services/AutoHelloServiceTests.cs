using System.Reflection;
using MARS.Shared.Concurrency;
using MARS.WaifuGacha.Entities;
using MARS.WaifuGacha.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.WaifuGacha.Tests.Services;

/// <summary>
/// Авто-приветствие от супруга.
///
/// Правила, из-за которых сервис и написан, проверяются по отдельности: брак,
/// включённый флаг и двадцать часов с прошлого приветствия. Без последнего
/// проверки зритель получал бы приветствие на каждом сообщении, а без флага —
/// даже там, где пользователь его выключил.
/// </summary>
public class AutoHelloServiceTests : IDisposable
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly AutoHelloService _service;

    public AutoHelloServiceTests()
    {
        _service = new AutoHelloService(
            _factory,
            new WeddingAnniversaryService(_factory, NullLogger<WeddingAnniversaryService>.Instance),
            new KeyedAsyncLock(),
            NullLogger<AutoHelloService>.Instance
        );
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task MarriedUserGetsGreeting()
    {
        await SeedAsync(
            autoHelloEnabled: true,
            lastGreetingHoursAgo: 100,
            messages: ["Привет, {user}!"]
        );

        var message = await _service.GetAutoHelloMessageAsync(
            "123456789",
            "Pyro",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(message);
        Assert.Contains("Pyro", message);
        Assert.Contains("Аква", message);
    }

    /// <summary>
    /// Токен <c>{randomHost}</c> подменяется нейтральной фразой: WaifuGacha не
    /// владеет таблицей Twitch-пользователей, и в чат утек бы необработанный
    /// токен. Проверяется сама подстановка — фраза выбирается случайно из
    /// засеянной базы, и в сообщение могла бы попасть любая из ста.
    /// </summary>
    [Theory]
    [InlineData("Привет, {randomHost}!", true)]
    [InlineData("Просто фраза", false)]
    [InlineData("", false)]
    public void RandomHostTokenIsReplaced(string message, bool expectsFallback)
    {
        var result = (string)ResolveRandomHost(message);

        Assert.DoesNotContain("{randomHost}", result);
        Assert.Equal(expectsFallback, result.Contains("один из зрителей"));
    }

    /// <summary>
    /// Приветствие не выдаётся раньше двадцати часов: иначе авто-приветствие
    /// сыпалось бы на каждое сообщение в чате.
    /// </summary>
    [Fact]
    public async Task GreetingWithinCooldownIsSkipped()
    {
        await SeedAsync(autoHelloEnabled: true, lastGreetingHoursAgo: 1, messages: ["Привет!"]);

        var message = await _service.GetAutoHelloMessageAsync(
            "123456789",
            "Pyro",
            TestContext.Current.CancellationToken
        );

        Assert.Null(message);
    }

    [Fact]
    public async Task DisabledAutoHelloIsSkipped()
    {
        await SeedAsync(autoHelloEnabled: false, lastGreetingHoursAgo: 100, messages: ["Привет!"]);

        var message = await _service.GetAutoHelloMessageAsync(
            "123456789",
            "Pyro",
            TestContext.Current.CancellationToken
        );

        Assert.Null(message);
    }

    /// <summary>
    /// Неприватизированный пользователь приветствий не получает: показ личного
    /// сообщения в общем чате не должен происходить.
    /// </summary>
    [Fact]
    public async Task PublicUserGetsNoGreeting()
    {
        await SeedAsync(
            autoHelloEnabled: true,
            lastGreetingHoursAgo: 100,
            messages: ["Привет!"],
            isPrivated: false
        );

        var message = await _service.GetAutoHelloMessageAsync(
            "123456789",
            "Pyro",
            TestContext.Current.CancellationToken
        );

        Assert.Null(message);
    }

    /// <summary>
    /// На годовщину вместо обычной фразы приходит поздравление: ради этого
    /// сервис и спрашивает <see cref="WeddingAnniversaryService"/>.
    /// </summary>
    [Fact]
    public async Task AnniversaryReplacesGreeting()
    {
        await SeedAsync(
            autoHelloEnabled: true,
            lastGreetingHoursAgo: 100,
            messages: ["Привет!"],
            marriedMonthsAgo: 12,
            lastCongratulatedMonths: -1
        );

        var message = await _service.GetAutoHelloMessageAsync(
            "123456789",
            "Pyro",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(message);
        Assert.Contains("поздравляет", message);
    }

    [Fact]
    public async Task GreetingTimeIsSaved()
    {
        await SeedAsync(autoHelloEnabled: true, lastGreetingHoursAgo: 100, messages: ["Привет!"]);

        await _service.GetAutoHelloMessageAsync(
            "123456789",
            "Pyro",
            TestContext.Current.CancellationToken
        );

        Assert.True(await GreetingTimeWasRefreshedAsync());
    }

    /// <summary>
    /// Без фраз в базе приветствие всё равно формируется — пустым текстом внутри
    /// сообщения. Молча не отвечать нельзя: супруг «поздоровался» вхолостую, и
    /// пользователь решил бы, что у него отключили приветствия.
    /// </summary>
    [Fact]
    public async Task MissingPhrasesStillProduceMessage()
    {
        await SeedAsync(autoHelloEnabled: true, lastGreetingHoursAgo: 100, messages: []);

        var message = await _service.GetAutoHelloMessageAsync(
            "123456789",
            "Pyro",
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(message);
    }

    [Fact]
    public async Task TogglingCreatesRecordForUnknownUser()
    {
        var enabled = await _service.ToggleAutoHelloEnabledAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.True(enabled);
        Assert.True(await IsAutoHelloEnabledAsync());
    }

    [Fact]
    public async Task TogglingTwiceTurnsAutoHelloOff()
    {
        await _service.ToggleAutoHelloEnabledAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        var secondToggle = await _service.ToggleAutoHelloEnabledAsync(
            "123456789",
            TestContext.Current.CancellationToken
        );

        Assert.False(secondToggle);
        Assert.False(await IsAutoHelloEnabledAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankArgumentsAreRejected(string twitchId)
    {
        Assert.Null(
            await _service.GetAutoHelloMessageAsync(
                twitchId,
                "Pyro",
                TestContext.Current.CancellationToken
            )
        );
        Assert.Null(
            await _service.GetAutoHelloMessageAsync(
                "123456789",
                twitchId,
                TestContext.Current.CancellationToken
            )
        );
        Assert.False(
            await _service.ToggleAutoHelloEnabledAsync(
                twitchId,
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Подстановка внутренняя и статическая: вызывается так же, как её зовёт
    /// сборка сообщения.
    /// </summary>
    private static string ResolveRandomHost(string message) =>
        (string)
            typeof(AutoHelloService)
                .GetMethod("ResolveRandomHost", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [message])!;

    private async Task<bool> IsAutoHelloEnabledAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );

        return await db
            .Husbands.Select(h => h.IsAutoHelloEnabled)
            .FirstAsync(TestContext.Current.CancellationToken);
    }

    private async Task<bool> GreetingTimeWasRefreshedAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        var greeting = await db.HusbandGreetings.FirstAsync(TestContext.Current.CancellationToken);

        return greeting.Time > DateTime.UtcNow.AddMinutes(-5);
    }

    private async Task SeedAsync(
        bool autoHelloEnabled,
        double lastGreetingHoursAgo,
        string[] messages,
        bool isPrivated = true,
        int marriedMonthsAgo = 0,
        int? lastCongratulatedMonths = 0
    )
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        // Одна и та же запись приветствия и в DbSet, и в навигации: сервис
        // читает её через Include, и две разные записи означали бы, что время
        // последнего приветствия не то, что задумано.
        var greeting = new HusbandAutoHello
        {
            HusbandId = "123456789",
            Time = DateTime.UtcNow.AddHours(-lastGreetingHoursAgo),
        };
        var coolDown = new HusbandCoolDown { HusbandId = "123456789" };

        db.Waifus.Add(
            new Waifu
            {
                ShikiId = "waifu-1",
                Name = "Аква",
                ImageUrl = "https://example.org/a.png",
            }
        );
        db.HusbandGreetings.Add(greeting);
        db.HusbandCoolDowns.Add(coolDown);
        db.Husbands.Add(
            new Husband
            {
                TwitchId = "123456789",
                WhenOrdered = DateTime.UtcNow.AddMonths(-marriedMonthsAgo),
                WhenPrivated = DateTime.UtcNow.AddMonths(-marriedMonthsAgo),
                IsPrivated = isPrivated,
                IsAutoHelloEnabled = autoHelloEnabled,
                WaifuBrideId = "waifu-1",
                LastWeddingCongratulatedMonths = lastCongratulatedMonths,
                HusbandGreetings = greeting,
                HusbandCoolDown = coolDown,
            }
        );

        for (var index = 0; index < messages.Length; index++)
        {
            db.AutoHelloMessages.Add(
                new AutoHelloMessage { Order = index, Text = messages[index] }
            );
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
