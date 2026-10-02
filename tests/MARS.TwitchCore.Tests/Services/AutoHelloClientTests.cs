using MARS.Shared.Clients;
using MARS.TwitchCore.Services.AutoHello;
using Moq;

namespace MARS.TwitchCore.Tests.Services;

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
        var waifu = new Mock<IWaifuGachaClient>();
        waifu
            .Setup(client => client.GetAutoHelloMessageAsync("123", "Pyro"))
            .ReturnsAsync("Привет");

        var client = new AutoHelloClient(waifu.Object);

        Assert.Equal("Привет", await client.GetAutoHelloMessageAsync("123", "Pyro"));
    }

    /// <summary>
    /// Без приветствия возвращается пусто: бот не должен отвечать на каждое
    /// сообщение.
    /// </summary>
    [Fact]
    public async Task MissingGreetingIsPassedThrough()
    {
        var waifu = new Mock<IWaifuGachaClient>();
        waifu
            .Setup(client =>
                client.GetAutoHelloMessageAsync(It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync((string?)null);

        var client = new AutoHelloClient(waifu.Object);

        Assert.Null(await client.GetAutoHelloMessageAsync("123", "Pyro"));
    }
}
