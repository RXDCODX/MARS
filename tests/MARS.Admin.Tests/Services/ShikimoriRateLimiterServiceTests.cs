using Grpc.Core;
using MARS.Admin.Application.Services;
using MARS.Admin.Infrastructure.Shikimori;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Shikimori;
using MARS.Shared.Contracts.Clients;
using Moq;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Просмотр состояния рейт-лимитера Shikimori: сервис админки лишь проксирует
/// ответ gRPC-клиента, поэтому проверяется передача запроса и проброс результата без
/// сетевого обращения к Shikimori.
/// </summary>
public class ShikimoriRateLimiterServiceTests
{
    [Fact]
    public async Task InfoIsTakenFromShikimoriClient()
    {
        var expected = new ShikimoriRateLimiterInfo(5, 300, 1.5, 30.5);
        var shikimori = new Mock<IShikimoriApi>();
        shikimori
            .Setup(instance => instance.GetRateLimiterInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.Shikimori).Returns(shikimori.Object);
        var service = new ShikimoriRateLimiterService(grpc.Object);

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
        var shikimori = new Mock<IShikimoriApi>();
        shikimori
            .Setup(instance => instance.GetRateLimiterInfoAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<ShikimoriRateLimiterInfo>(null!));
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.Shikimori).Returns(shikimori.Object);
        var service = new ShikimoriRateLimiterService(grpc.Object);

        Assert.Null(await service.GetRateLimiterInfoAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClientFailureIsNotSwallowed()
    {
        var failure = new RpcException(new Status(StatusCode.Unavailable, "shikimori down"));
        var shikimori = new Mock<IShikimoriApi>();
        shikimori
            .Setup(instance => instance.GetRateLimiterInfoAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.Shikimori).Returns(shikimori.Object);
        var service = new ShikimoriRateLimiterService(grpc.Object);

        var actual = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetRateLimiterInfoAsync(TestContext.Current.CancellationToken)
        );

        Assert.Same(failure, actual);
    }
}
