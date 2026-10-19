using MARS.Telegram.Configuration;
using MARS.Telegram.Services.BotService;
using MARS.Telegram.Services.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TL;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Отправка сообщений ботом по MTProto.
/// </summary>
/// <remarks>
/// Проверяется решение отправителя: скольким администраторам ушло и что ушло.
/// Настоящий клиент не подменяется методами — они невиртуальные, — поэтому
/// используется узкая обёртка <see cref="ITelegramBotConnection"/>.
/// </remarks>
public class MTProtoMessageSenderTests
{
    /// <summary>
    /// Главный случай: сообщение уходит каждому администратору из конфигурации.
    /// Пропуск одного означал бы «тихую» потерю просьбы.
    /// </summary>
    [Fact]
    public async Task Сообщение_Уходит_Всем_Администраторам()
    {
        var connection = Connection();
        var sender = Create(connection.Object, [402763435, 42]);

        var delivered = await sender.SendToAdminsAsync("ссылка", Token);

        Assert.Equal(2, delivered);
        connection.Verify(
            item =>
                item.SendTextAsync(
                    It.IsAny<InputPeer>(),
                    It.Is<string>(text => text.Contains("ссылка")),
                    It.IsAny<MessageEntity[]?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(2)
        );
    }

    /// <summary>
    /// Пустой список администраторов — «слать некому»: отправитель не падает и не
    /// делает вид, что доставил.
    /// </summary>
    [Fact]
    public async Task Без_Администраторов_Никто_Не_Получает()
    {
        var sender = Create(Connection().Object, []);

        var delivered = await sender.SendToAdminsAsync("ссылка", Token);

        Assert.Equal(0, delivered);
    }

    /// <summary>
    /// Ошибка доставки не выходит наружу: вызывающий — это gRPC-приёмник, и
    /// исключение у него стало бы «Неизвестная ошибка» вместо честного статуса.
    /// </summary>
    [Fact]
    public async Task Ошибка_Доставки_Не_Поднимается_Наружу()
    {
        var connection = Connection();
        connection
            .Setup(item =>
                item.SendTextAsync(
                    It.IsAny<InputPeer>(),
                    It.IsAny<string>(),
                    It.IsAny<MessageEntity[]?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("Telegram недоступен"));
        var sender = Create(connection.Object, [42]);

        var delivered = await sender.SendToAdminsAsync("ссылка", Token);

        Assert.Equal(0, delivered);
    }

    /// <summary>
    /// Вход не состоялся (нет токена, нет сети до прокси) — доставки тоже нет, и
    /// наружу это не выходит: сессия отказывает исключением, а вызывающий — это
    /// gRPC-приёмник, для которого исключение стало бы «Неизвестная ошибка».
    /// </summary>
    [Fact]
    public async Task Неудачный_Вход_Не_Доставляет()
    {
        var connection = Connection();
        var session = new Mock<ITelegramBotSession>();
        session
            .Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Бот не настроен"));
        var sender = new MTProtoMessageSender(
            session.Object,
            Options.Create(new TelegramConfiguration { AdminIds = [42] }),
            NullLogger<MTProtoMessageSender>.Instance
        );

        var delivered = await sender.SendToAdminsAsync("ссылка", Token);

        Assert.Equal(0, delivered);
        connection.Verify(
            item =>
                item.SendTextAsync(
                    It.IsAny<InputPeer>(),
                    It.IsAny<string>(),
                    It.IsAny<MessageEntity[]?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// Разметка уходит сущностями: у Bot API её разбирал Telegram, теперь это
    /// делает сервис, иначе в чате появились бы теги.
    /// </summary>
    [Fact]
    public async Task Разметка_Уходит_Сущностями()
    {
        var connection = Connection();
        var sender = Create(connection.Object, [42]);

        await sender.SendToAdminsAsync("<b>готово</b>", Token);

        connection.Verify(
            item =>
                item.SendTextAsync(
                    It.IsAny<InputPeer>(),
                    "готово",
                    It.Is<MessageEntity[]>(entities => entities.Length == 1),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Разметка, которую разобрать нельзя, не уходит вовсе: слать её «как есть»
    /// означало бы отправить в чат сырые теги.
    /// </summary>
    [Fact]
    public async Task Неразбираемая_Разметка_Не_Отправляется()
    {
        var connection = Connection();
        var sender = Create(connection.Object, [42]);

        var delivered = await sender.SendToAdminsAsync("текст <blink>мигает</blink>", Token);

        Assert.Equal(0, delivered);
        connection.Verify(
            item =>
                item.SendTextAsync(
                    It.IsAny<InputPeer>(),
                    It.IsAny<string>(),
                    It.IsAny<MessageEntity[]?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Mock<ITelegramBotConnection> Connection() => new(MockBehavior.Loose);

    private static MTProtoMessageSender Create(
        ITelegramBotConnection connection,
        long[] admins,
        string botToken = "123456:токен"
    )
    {
        var session = new Mock<ITelegramBotSession>();
        session
            .Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);

        return new MTProtoMessageSender(
            session.Object,
            Options.Create(
                new TelegramConfiguration
                {
                    AdminIds = admins,
                    BotToken = botToken,
                    ApiId = "1234567",
                    ApiHash = "0123456789abcdef0123456789abcdef",
                }
            ),
            NullLogger<MTProtoMessageSender>.Instance
        );
    }
}
