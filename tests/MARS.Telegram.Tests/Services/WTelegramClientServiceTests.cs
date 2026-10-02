using MARS.Telegram.Entities;
using MARS.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Клиент WTelegram в автономном микросервисе.
///
/// Авторизация MTProto в этом сервисе не поддерживается: ключи Telegram живут в
/// другом микросервисе. Проверяется именно это поведение — без него администратор
/// нажал бы «переавторизовать» и получил бы исключение вместо внятного отказа.
/// </summary>
public class WTelegramClientServiceTests
{
    [Fact]
    public async Task FreshServiceIsNotAuthenticated()
    {
        var status = await Create().GetClientStatusAsync(Token);

        Assert.False(status.IsAuthenticated);
        Assert.False(status.IsAwaitingCode);
    }

    /// <summary>
    /// Повторная авторизация не поддерживается и сообщает об этом прямо.
    /// </summary>
    [Fact]
    public async Task ReLoginIsNotSupported()
    {
        var service = Create();

        await Assert.ThrowsAsync<NotSupportedException>(() => service.ReLoginAsync(Token));
    }

    /// <summary>
    /// Ввод кода подтверждения тоже недоступен: он относится к авторизации, которой
    /// здесь нет.
    /// </summary>
    [Fact]
    public void VerificationCodeIsRejected()
    {
        Assert.False(Create().SubmitVerificationCode("12345"));
    }

    /// <summary>
    /// Без ключей API клиент не создаётся: подключаться нечем, и понятная ошибка
    /// лучше молчаливого таймаута.
    /// </summary>
    [Fact]
    public async Task MissingCredentialsAreReported()
    {
        var service = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetClientAsync(Token));
    }

    /// <summary>
    /// Обработка апдейтов пустая: события MTProto приходят по своему каналу, а
    /// через Bot API их не бывает.
    /// </summary>
    [Fact]
    public async Task HandleUpdateDoesNothing()
    {
        await Create().HandleUpdate(Mock.Of<ITelegramBotClient>(), null);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static WTelegramClientService Create() =>
        new(
            NullLogger<WTelegramClientService>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection().Build()
        );
}
