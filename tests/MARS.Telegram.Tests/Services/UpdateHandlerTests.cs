using MARS.Telegram.Configuration;
using MARS.Telegram.Data;
using MARS.Telegram.Services;
using MARS.Telegram.Services.BotService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Обработка апдейтов Telegram-бота.
///
/// Здесь три вещи, каждая видна администратору: пересылка сообщений админам,
/// сохранение смещения (иначе после перезапуска сообщения повторялись бы) и
/// рассылка апдейта подписчикам. Проверяются все три, включая отказ Telegram: одно
/// недоступное сообщение не должно ронять обработку всего апдейта.
/// </summary>
public class UpdateHandlerTests
{
    private readonly ChatTestDbContextFactory _factory = new();
    private readonly Mock<ITelegramBotClient> _bot = new();

    [Fact]
    public async Task MessageIsForwardedToAdmins()
    {
        var handler = Create([777]);

        await handler.HandleUpdateAsync(
            _bot.Object,
            Message(chatId: -100, messageId: 42, updateId: 5),
            Token
        );

        Assert.True(Forwarded(toChatId: 777, fromChatId: -100, messageId: 42));
    }

    /// <summary>
    /// Защищённое содержимое не пересылается: это нарушение приватности автора.
    /// </summary>
    [Fact]
    public async Task ProtectedMessageIsNotForwarded()
    {
        var handler = Create([777]);
        var update = Message(chatId: -100, messageId: 42, updateId: 5);
        update.Message!.HasProtectedContent = true;

        await handler.HandleUpdateAsync(_bot.Object, update, Token);

        Assert.Empty(ForwardRequests());
    }

    /// <summary>
    /// Сообщение канала пересылается так же, как сообщение чата: администратор
    /// следит и за каналами.
    /// </summary>
    [Fact]
    public async Task ChannelPostIsForwardedToAdmins()
    {
        var handler = Create([777]);

        await handler.HandleUpdateAsync(
            _bot.Object,
            ChannelPost(chatId: -100, messageId: 43, updateId: 6),
            Token
        );

        Assert.True(Forwarded(toChatId: 777, fromChatId: -100, messageId: 43));
    }

    /// <summary>
    /// Отказ Telegram по конкретному сообщению не мешает сохранению смещения: иначе
    /// бот повторял бы одно и то же сообщение до бесконечности.
    /// </summary>
    [Fact]
    public async Task OffsetIsStoredEvenWhenForwardFails()
    {
        _bot.Setup(client =>
                client.SendRequest(It.IsAny<ForwardMessageRequest>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new ApiRequestException("message to forward not found", 400));
        var handler = Create([777]);

        await handler.HandleUpdateAsync(
            _bot.Object,
            Message(chatId: -100, messageId: 42, updateId: 11),
            Token
        );

        await using var db = await _factory.CreateDbContextAsync(Token);
        var offset = await db.TelegramUpdateReceiverOffsets.SingleOrDefaultAsync(Token);
        Assert.Equal(11, offset!.Offset);
    }

    /// <summary>
    /// Смещение двигается вперёд: без этого сообщения после перезапуска повторялись
    /// бы заново.
    /// </summary>
    [Fact]
    public async Task OffsetAdvancesWithUpdates()
    {
        var handler = Create([777]);

        await handler.HandleUpdateAsync(
            _bot.Object,
            Message(chatId: -100, messageId: 42, updateId: 20),
            Token
        );

        await using var db = await _factory.CreateDbContextAsync(Token);
        var offset = await db.TelegramUpdateReceiverOffsets.SingleOrDefaultAsync(Token);
        Assert.Equal(20, offset!.Offset);
    }

    /// <summary>
    /// Подписчики получают апдейт: на этом стоят буфер обмена и эффекты оверлея.
    /// </summary>
    [Fact]
    public async Task SubscribersReceiveTheUpdate()
    {
        var received = new List<long>();
        var handler = Create([777]);
        handler.TelegramUpdate += (_, update) =>
        {
            received.Add(update.Id);

            return Task.CompletedTask;
        };
        var update = Message(chatId: -100, messageId: 42, updateId: 7);

        await handler.HandleUpdateAsync(_bot.Object, update, Token);

        Assert.Equal([update.Id], received);
    }

    /// <summary>
    /// Ошибка от Telegram только пишется в журнал: обработчик продолжает опрос.
    /// </summary>
    [Fact]
    public async Task ErrorHandlerOnlyLogs()
    {
        await Create([777])
            .HandleErrorAsync(
                _bot.Object,
                new InvalidOperationException("ошибка"),
                HandleErrorSource.PollingError,
                Token
            );
    }

    /// <summary>
    /// Ошибка опроса не роняет поллинг: попытка повторяется.
    /// </summary>
    [Fact]
    public async Task PollingErrorIsSurvived()
    {
        var handler = Create([777]);

        await handler.HandlePollingErrorAsync(new InvalidOperationException("сеть"), Token);
        await handler.HandlePollingErrorAsync(null!, Token);
    }

    /// <summary>
    /// Без апдейта ничего не происходит: пустой апдейт — обычное состояние поллинга.
    /// </summary>
    [Fact]
    public async Task EmptyUpdateIsIgnored()
    {
        await Create([777]).HandleUpdateAsync(_bot.Object, null!, Token);

        Assert.Empty(ForwardRequests());
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private UpdateHandler Create(long[] adminIds) =>
        new(
            _bot.Object,
            NullLogger<UpdateHandler>.Instance,
            Options.Create(new TelegramConfiguration { AdminIdsArray = adminIds }),
            new StubLifetime(),
            _factory,
            Mock.Of<ITelegramClipboardCopyService>(),
            []
        );

    /// <summary>
    /// <c>ForwardMessage</c> в Telegram.Bot 22 — расширяющий метод, поэтому
    /// перехватывается сам запрос клиента.
    /// </summary>
    private IEnumerable<ForwardMessageRequest> ForwardRequests() =>
        _bot
            .Invocations.Select(invocation => invocation.Arguments.FirstOrDefault())
            .OfType<ForwardMessageRequest>();

    private bool Forwarded(long toChatId, long fromChatId, int messageId) =>
        ForwardRequests()
            .Any(request =>
                request.ChatId == toChatId
                && request.FromChatId == fromChatId
                && request.MessageId == messageId
            );

    private static Update Message(long chatId, int messageId, int updateId) =>
        new()
        {
            Id = updateId,
            Message = new Message
            {
                Id = messageId,
                Chat = new Chat { Id = chatId },
            },
        };

    private static Update ChannelPost(long chatId, int messageId, int updateId) =>
        new()
        {
            Id = updateId,
            ChannelPost = new Message
            {
                Id = messageId,
                Chat = new Chat { Id = chatId },
            },
        };

    /// <summary>
    /// Время жизни без событий: подписки на апдейты добавляются после старта
    /// приложения, а тест проверяет обработчик напрямую.
    /// </summary>
    private sealed class StubLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() { }
    }
}
