using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Services.WeddingAnniversary;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Годовщины свадьбы в MARS.TwitchCore.
///
/// Сервис отвечает только за вопрос «какая годовщина ближе всего по всем
/// пользователям» — это единственный его потребитель, эндпоинт статистики.
/// Отправка поздравлений и отметка отправленного живут в одноимённом сервисе
/// MARS.WaifuGacha, куда их перенесли при разделении сервисов; здесь таких
/// методов нет, и обещать проверку «ровно один раз» было бы враньём — отмечать
/// нечего.
///
/// Поле `LastWeddingCongratulatedMonths` здесь только читается, и пишет его
/// сервис из MARS.WaifuGacha. Поэтому «уже поздравлено» проверяется чтением
/// чужой отметки, а не собственным состоянием.
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
