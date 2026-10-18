using MARS.Telegram.Configuration;
using MARS.Telegram.Services;
using MARS.Telegram.Services.WTelegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WTelegramClient = WTelegram.Client;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Клиент WTelegram в автономном микросервисе.
/// </summary>
/// <remarks>
/// Раньше здесь утверждалось, что авторизация не поддерживается: ключи Telegram
/// лежали в другом микросервисе. Теперь сессия хранится в базе этого сервиса, а
/// вход идёт через прокси, поэтому переподключиться можно без участия человека —
/// и проверяется именно это.
/// </remarks>
public class WTelegramClientServiceTests
{
    /// <summary>Настоящий api_hash: 32 hex-символа, из него выводится ключ сессии.</summary>
    private const string ApiHash = "0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task FreshServiceIsNotAuthenticated()
    {
        var status = await Create(Mock.Of<IWTelegramClientFactory>()).GetClientStatusAsync(Token);

        Assert.False(status.IsAuthenticated);
        Assert.False(status.IsAwaitingCode);
    }

    /// <summary>
    /// Клиент без авторизации в памяти не остаётся: он всё равно ничего не умеет,
    /// и следующий вызов обязан снова попробовать войти — иначе после неудачного
    /// входа сервис молчал бы навсегда. Проверить обратное (что авторизованный
    /// клиент кэшируется) можно только на живом входе: <c>User</c> у клиента
    /// только для чтения, подделать его в тесте нельзя.
    /// </summary>
    [Fact]
    public async Task Клиент_Без_Авторизации_Не_Остаётся_В_Памяти()
    {
        var factory = new Mock<IWTelegramClientFactory>();
        factory.Setup(item => item.LoginAsUserAsync(Token)).ReturnsAsync(NewClient());

        var service = Create(factory.Object);

        await service.GetClientAsync(Token);
        await service.GetClientAsync(Token);

        factory.Verify(item => item.LoginAsUserAsync(Token), Times.Exactly(2));
    }

    /// <summary>
    /// Переподключение закрывает текущий клиент и входит заново: иначе клиент после
    /// потери сессии остался бы в памяти и следующая попытка упала бы на нём же.
    /// </summary>
    [Fact]
    public async Task Переподключение_Закрывает_Клиента_И_Входит_Заново()
    {
        var factory = new Mock<IWTelegramClientFactory>();
        factory.Setup(item => item.LoginAsUserAsync(Token)).ReturnsAsync(NewClient());

        var service = Create(factory.Object);

        await service.GetClientAsync(Token);
        await service.ReLoginAsync(Token);

        var status = await service.GetClientStatusAsync(Token);

        Assert.False(status.IsAuthenticated);
        factory.Verify(item => item.LoginAsUserAsync(Token), Times.Exactly(2));
    }

    /// <summary>
    /// Ввод кода подтверждения недоступен: он относится к авторизации, которой
    /// здесь нет, и принимать его молча было бы хуже отказа.
    /// </summary>
    [Fact]
    public void VerificationCodeIsRejected()
    {
        Assert.False(Create(Mock.Of<IWTelegramClientFactory>()).SubmitVerificationCode("12345"));
    }

    /// <summary>
    /// Без учётных данных приложения клиент не создаётся: подключаться нечем, и
    /// внятная ошибка с именем ключа лучше молчаливого таймаута.
    /// </summary>
    [Fact]
    public async Task MissingCredentialsAreReported()
    {
        var factory = new WTelegramClientFactory(
            Options.Create(new TelegramConfiguration()),
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            new ChatTestDbContextFactory(),
            NullLogger<WTelegramClientFactory>.Instance
        );

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(factory).GetClientAsync(Token)
        );

        Assert.Contains("Telegram__ApiId", error.Message);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>
    /// Клиент без входа: конструктор обращается только к конфигурации приложения и
    /// создаёт пустой поток сессии, сети на нём нет.
    /// </summary>
    private static WTelegramClient NewClient() =>
        new(
            key =>
                key switch
                {
                    "api_id" => "1234567",
                    "api_hash" => ApiHash,
                    _ => null,
                },
            new MemoryStream()
        );

    private static WTelegramClientService Create(IWTelegramClientFactory factory) =>
        new(factory, NullLogger<WTelegramClientService>.Instance);
}
