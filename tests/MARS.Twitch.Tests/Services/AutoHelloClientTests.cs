using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.WaifuGacha;
using MARS.Twitch.Infrastructure.Services.AutoHello;
using Moq;

namespace MARS.Twitch.Tests.Services;

/// <summary>
/// Запрос приветствия у MARS.WaifuGacha.
///
/// Правила кулдауна и годовщины живут во владельце супругов. Здесь только
/// переадресация, и проверяется именно она: своя реализация кулдауна развела бы
/// два источника истины.
/// </summary>
public class AutoHelloClientTests
{
    [Fact]
    public async Task GreetingIsAskedFromWaifuService()
    {
        var waifuGacha = new Mock<IWaifuGachaApi>();
        waifuGacha
            .Setup(client => client.GetAutoHelloMessageAsync("123", "Pyro"))
            .ReturnsAsync("Привет");
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.WaifuGacha).Returns(waifuGacha.Object);

        var client = new AutoHelloClient(grpc.Object);

        Assert.Equal("Привет", await client.GetAutoHelloMessageAsync("123", "Pyro"));
    }

    /// <summary>
    /// Без приветствия возвращается пусто: бот не должен отвечать на каждое
    /// сообщение.
    /// </summary>
    [Fact]
    public async Task MissingGreetingIsPassedThrough()
    {
        var waifuGacha = new Mock<IWaifuGachaApi>();
        waifuGacha
            .Setup(client =>
                client.GetAutoHelloMessageAsync(It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync((string?)null);
        var grpc = new Mock<IMarsGrpcClient>();
        grpc.SetupGet(client => client.WaifuGacha).Returns(waifuGacha.Object);

        var client = new AutoHelloClient(grpc.Object);

        Assert.Null(await client.GetAutoHelloMessageAsync("123", "Pyro"));
    }
}
