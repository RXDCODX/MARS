using MARS.Admin.Services;
using MARS.Shared.Clients;
using Moq;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Просмотр состояния рейт-лимитера Shikimori: сервис админки лишь проксирует
/// ответ клиента, поэтому проверяется передача запроса и проброс результата без
/// сетевого обращения к Shikimori.
/// </summary>
public class ShikimoriRateLimiterServiceTests
{
    [Fact]
    public async Task InfoIsTakenFromShikimoriClient()
    {
        var expected = new ShikimoriRateLimiterInfo(5, 300, 1.5, 30.5);
        var client = new Mock<IShikimoriApiClient>();
        client
            .Setup(instance => instance.GetRateLimiterInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var service = new ShikimoriRateLimiterService(client.Object);

        var result = await service.GetRateLimiterInfoAsync(TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        Assert.Equal(5, result!.AvailablePerSecond);
        Assert.Equal(300, result.AvailablePerMinute);
        Assert.Equal(30.5, result.SecondsToResetMinuteWindow);
    }

    /// <summary>
    /// Отсутствие информации — это null, а не пустой объект с нулями: иначе панель
    /// показала бы «лимит 0» вместо «сервис недоступен».
    /// </summary>
    [Fact]
    public async Task MissingInfoStaysNull()
    {
        var client = new Mock<IShikimoriApiClient>();
        client
            .Setup(instance => instance.GetRateLimiterInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShikimoriRateLimiterInfo?)null);
        var service = new ShikimoriRateLimiterService(client.Object);

        Assert.Null(await service.GetRateLimiterInfoAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClientFailureIsNotSwallowed()
    {
        var client = new Mock<IShikimoriApiClient>();
        client
            .Setup(instance => instance.GetRateLimiterInfoAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("shikimori down"));
        var service = new ShikimoriRateLimiterService(client.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetRateLimiterInfoAsync(TestContext.Current.CancellationToken)
        );
    }
}
