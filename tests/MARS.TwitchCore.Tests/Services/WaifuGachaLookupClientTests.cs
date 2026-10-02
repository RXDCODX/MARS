using MARS.Shared.Clients;
using MARS.TwitchCore.Services.Rewards;
using Moq;

namespace MARS.TwitchCore.Tests.Services;

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
        var waifu = new Mock<IWaifuGachaClient>();
        waifu
            .Setup(client => client.GetWaifuNameForUserAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Аяка");

        var client = new WaifuGachaLookupClient(waifu.Object);

        Assert.Equal(
            "Аяка",
            await client.GetWaifuNameForUserAsync("123", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UnknownUserHasNoWaifu()
    {
        var waifu = new Mock<IWaifuGachaClient>();
        waifu
            .Setup(client =>
                client.GetWaifuNameForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((string?)null);

        var client = new WaifuGachaLookupClient(waifu.Object);

        Assert.Null(
            await client.GetWaifuNameForUserAsync("999", TestContext.Current.CancellationToken)
        );
    }
}
