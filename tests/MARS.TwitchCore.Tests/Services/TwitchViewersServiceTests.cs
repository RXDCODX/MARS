using MARS.TwitchCore.Services.TwitchFollowers;
using Moq;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Списки зрителей канала: подписчики, VIP и модераторы.
///
/// Проверяется главное контрактное правило этих методов: без токена в базе они
/// возвращают null и не ходят в Twitch. Смысл в том, что вызывающий код отличает
/// «список пуст» от «список неизвестен» — иначе после простоя токена канал молча
/// выглядел бы пустым, и подписчики стёрлись бы из базы.
/// </summary>
public class TwitchViewersServiceTests
{
    [Fact]
    public async Task FollowersAreNotFetchedWithoutToken()
    {
        var service = Create();

        Assert.Null(await service.GetAllFollowers());
    }

    [Fact]
    public async Task VipsAreNotFetchedWithoutToken()
    {
        var service = Create();

        Assert.Null(await service.GetAllViPs());
    }

    [Fact]
    public async Task ModeratorsAreNotFetchedWithoutToken()
    {
        var service = Create();

        Assert.Null(await service.GetModerators());
    }

    /// <summary>
    /// Сервис не обращается в Twitch, когда токена нет: при пустом
    /// <c>Helix</c> любой запрос упал бы, и «нет токена» отличалось бы от
    /// «Twitch недоступен» только стектрейсом.
    /// </summary>
    [Fact]
    public async Task NoRequestIsSentWithoutToken()
    {
        var api = new Mock<ITwitchAPI>();

        var service = new TwitchViewersService(api.Object, TwitchTokenServiceStub.WithoutToken());
        await service.GetAllFollowers();
        await service.GetAllViPs();
        await service.GetModerators();

        api.VerifyGet(instance => instance.Helix, Times.Never);
    }

    private static TwitchViewersService Create() =>
        new(Mock.Of<ITwitchAPI>(), TwitchTokenServiceStub.WithoutToken());
}
