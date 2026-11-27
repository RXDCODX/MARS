using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.WaifuGacha;
using MARS.Twitch.Infrastructure.Services.Rewards;
using Moq;

namespace MARS.Twitch.Tests.Services;

/// <summary>
/// Поиск супруга для русской рулетки.
///
/// Имя супруга знает только MARS.WaifuGacha, поэтому запрос идёт туда напрямую, а
/// не через RabbitMQ-ответы. Проверяется переадресация и то, что отказ приходит как
/// пустое имя, а не как исключение.
/// </summary>
public class WaifuGachaLookupClientTests
{
    [Fact]
    public async Task WaifuNameIsLookedUpInWaifuService()
    {
        var waifuGacha = new Mock<IWaifuGachaApi>();
        waifuGacha
            .Setup(client => client.GetWaifuNameForUserAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Аяка");
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.WaifuGacha).Returns(waifuGacha.Object);

        var client = new WaifuGachaLookupClient(grpc.Object);

        Assert.Equal(
            "Аяка",
            await client.GetWaifuNameForUserAsync("123", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UnknownUserHasNoWaifu()
    {
        var waifuGacha = new Mock<IWaifuGachaApi>();
        waifuGacha
            .Setup(client =>
                client.GetWaifuNameForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((string?)null);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.WaifuGacha).Returns(waifuGacha.Object);

        var client = new WaifuGachaLookupClient(grpc.Object);

        Assert.Null(
            await client.GetWaifuNameForUserAsync("999", TestContext.Current.CancellationToken)
        );
    }
}
