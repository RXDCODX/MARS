using MARS.Telegram.Services;
using MARS.Telegram.Services.WTelegram;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WTelegramClient = WTelegram.Client;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Передача кода подтверждения входа в учётную запись пользователя.
/// </summary>
/// <remarks>
/// <para>
/// Код приходит из веб-страницы «WTelegram Клиент»: клиент требует его при первом
/// входе, а читать код из приложения он не умеет. Поэтому код принимается
/// снаружи и уходит в клиент при следующей попытке входа.
/// </para>
/// <para>
/// Проверяется решение на стороне сервиса: куда код попал, когда статус готов
/// показать форму и что происходит с пустым кодом. Сам вход уходит в сеть и
/// проверяется на живом стенде.
/// </para>
/// </remarks>
public class WTelegramVerificationCodeTests
{
    /// <summary>
    /// Пустой код не принимается: клиент отверг бы его так же, а форма показала бы
    /// «принято» и замолчала.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Пустой_Код_Не_Принимается(string? code)
    {
        var factory = new Mock<IWTelegramClientFactory>();
        var service = Create(factory.Object);

        var accepted = service.SubmitVerificationCode(code!);

        Assert.False(accepted);
        factory.Verify(item => item.SubmitLoginCode(It.IsAny<string>()), Times.Never);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Код уходит в фабрику, а не теряется в контроллере: оттуда он попадёт в
    /// конфигурацию клиента при следующем входе.
    /// </summary>
    [Fact]
    public void Код_Уходит_В_Фабрику_Клиента()
    {
        var factory = new Mock<IWTelegramClientFactory>();
        var service = Create(factory.Object);

        var accepted = service.SubmitVerificationCode(" 54321 ");

        Assert.True(accepted);
        factory.Verify(item => item.SubmitLoginCode("54321"), Times.Once);
    }

    /// <summary>
    /// Форма кода показывается только когда вход действительно упёрся в код.
    /// Раньше признак всегда был ложью, и форма не появлялась никогда — код было
    /// некуда вводить, хотя страница для этого и сделана.
    /// </summary>
    [Fact]
    public async Task Статус_Сообщает_Что_Ждёт_Код()
    {
        var factory = new Mock<IWTelegramClientFactory>();
        factory.SetupGet(item => item.AwaitingCode).Returns(true);

        var status = await Create(factory.Object).GetClientStatusAsync(Token);

        Assert.True(status.IsAwaitingCode);
        Assert.False(status.IsAuthenticated);
    }

    /// <summary>
    /// Пока вход не упёрся в код, форма лишняя: её появление означало бы, что сервис
    /// ждёт того, чего не ждёт.
    /// </summary>
    [Fact]
    public async Task Без_Ожидания_Кода_Статус_Его_Не_Показывает()
    {
        var factory = new Mock<IWTelegramClientFactory>();
        factory.SetupGet(item => item.AwaitingCode).Returns(false);

        var status = await Create(factory.Object).GetClientStatusAsync(Token);

        Assert.False(status.IsAwaitingCode);
    }

    /// <summary>
    /// Переавторизация сбрасывает ожидание кода: вход начинается заново, и код
    /// может прийти уже другой.
    /// </summary>
    [Fact]
    public async Task Переавторизация_Сбрасывает_Ожидание_Кода()
    {
        var factory = new Mock<IWTelegramClientFactory>();
        factory
            .Setup(item => item.LoginAsUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewClient());
        var service = Create(factory.Object);

        await service.ReLoginAsync(Token);

        factory.Verify(item => item.LoginAsUserAsync(Token), Times.Once);
    }

    /// <summary>
    /// Причина неудачи видна на странице: без неё администратор видел бы только
    /// крутилки, и отличать «неверный код» от «прокси недоступен» было бы нечем.
    /// </summary>
    [Fact]
    public async Task Статус_Показывает_Причину_Неудачи()
    {
        var factory = new Mock<IWTelegramClientFactory>();
        factory.SetupGet(item => item.LastLoginError).Returns("400 PHONE_CODE_INVALID");

        var status = await Create(factory.Object).GetClientStatusAsync(Token);

        Assert.Equal("400 PHONE_CODE_INVALID", status.ErrorMessage);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static WTelegramClient NewClient() =>
        new(
            key =>
                key switch
                {
                    "api_id" => "1234567",
                    "api_hash" => "0123456789abcdef0123456789abcdef",
                    _ => null,
                },
            new MemoryStream()
        );

    private static WTelegramClientService Create(IWTelegramClientFactory factory) =>
        new(factory, NullLogger<WTelegramClientService>.Instance);
}
