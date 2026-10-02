using MARS.Admin.CustomLoggers.TelegramLogger;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;

namespace MARS.Admin.Tests;

/// <summary>
/// Отправка логов админки в Telegram.
///
/// Проверяется ровно то, что видит администратор: какой уровень проходит, как
/// выглядит строка и что длинное сообщение обрезается, а не уходит в Telegram
/// целиком (его лимит — 4096 символов).
///
/// Заглушка перехватывает не SendMessage, а SendRequest: в Telegram.Bot
/// SendMessage — расширяющий метод поверх интерфейса, и перечислять его
/// необязательные параметры ради проверки означало бы сломать тест при любом
/// обновлении пакета. Тело запроса доступно типизированно, через Text.
/// </summary>
public class TelegramLoggerTests
{
    private static readonly Mock<ITelegramBotClient> Bot = new(MockBehavior.Loose);

    [Fact]
    public async Task MessageReachesConfiguredChat()
    {
        var sent = CountSent();

        Logger(LogLevel.Trace).LogInformation("событие");

        await WaitUntil(() => sent() > 0);
        Assert.Contains("событие", LastText);
        Assert.Contains("Log source: MARS", LastText);
        Assert.Contains("MARS.Admin", LastText);
        Assert.Equal(42L, LastChatId);
    }

    /// <summary>
    /// Сообщение уходит всем адресатам из конфигурации: у администратора их
    /// несколько, и пропуск одного означал бы «тихую» потерю логов.
    /// </summary>
    [Fact]
    public async Task MessageGoesToEveryConfiguredChat()
    {
        var bot = new Mock<ITelegramBotClient>(MockBehavior.Loose);
        using var sender = new TelegramLoggerSender(bot.Object, [1L, 2L, 3L]);

        sender.EnqueueMessage("всем адресатам");

        await WaitUntil(() => bot.Invocations.Count >= 3);
        var chatIds = bot
            .Invocations.Select(invocation =>
                ((SendMessageRequest)invocation.Arguments[0]).ChatId.Identifier ?? 0
            )
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal([1L, 2L, 3L], chatIds);
    }

    /// <summary>
    /// Уровень ниже минимума не отправляется вовсе: иначе в чат администратора
    /// лился бы поток отладочных сообщений.
    /// </summary>
    [Fact]
    public async Task LevelsBelowMinimumAreDropped()
    {
        var sent = CountSent();
        var logger = Logger(LogLevel.Warning);

        logger.LogDebug("отладка");
        logger.LogInformation("информация");

        await Task.Delay(200, TestContext.Current.CancellationToken);
        Assert.Equal(0, sent());
    }

    [Theory]
    [InlineData(LogLevel.Trace, "trce")]
    [InlineData(LogLevel.Debug, "dbug")]
    [InlineData(LogLevel.Information, "info")]
    [InlineData(LogLevel.Warning, "warn")]
    [InlineData(LogLevel.Error, "fail")]
    [InlineData(LogLevel.Critical, "crit")]
    public async Task LevelIsWrittenWithKnownAbbreviation(LogLevel level, string expected)
    {
        var sent = CountSent();

        Logger(LogLevel.Trace).Log(level, "запись");

        await WaitUntil(() => sent() > 0);
        Assert.Contains(expected, LastText);
    }

    /// <summary>
    /// Уровня вне перечисления не бывает, но ветка Default в switch обязана
    /// давать пустую строку, а не «(Unknown)»: иначе в чат уехала бы выдумка.
    /// </summary>
    [Fact]
    public async Task NoneLevelIsWrittenWithoutAbbreviation()
    {
        var sent = CountSent();

        Logger(LogLevel.None).Log(LogLevel.None, "запись");

        await WaitUntil(() => sent() > 0);
        Assert.StartsWith(
            $"Log source: MARS{Environment.NewLine}{Environment.NewLine}MARS.Admin[0]",
            LastText
        );
    }

    [Fact]
    public async Task ExceptionIsAppendedToMessage()
    {
        var sent = CountSent();

        Logger(LogLevel.Trace).LogError(new InvalidOperationException("сбой"), "упало");

        await WaitUntil(() => sent() > 0);
        Assert.Contains("InvalidOperationException", LastText);
    }

    [Fact]
    public async Task EventIdIsWrittenIntoMessage()
    {
        var sent = CountSent();

        Logger(LogLevel.Trace).LogInformation(new EventId(77, "Событие"), "запись");

        await WaitUntil(() => sent() > 0);
        Assert.Contains("[77]", LastText);
    }

    /// <summary>
    /// Длинное сообщение обрезается до лимита Telegram: иначе отправка падала бы
    /// с ошибкой и в чат не пришло бы ничего — то есть потерялся бы ровно тот
    /// лог, который описывал проблему.
    /// </summary>
    [Fact]
    public async Task LongMessageIsTrimmedToTelegramLimit()
    {
        var sent = CountSent();

        Logger(LogLevel.Trace).LogInformation(new string('я', 8000));

        await WaitUntil(() => sent() > 0);
        Assert.True(
            LastText.Length <= 4096,
            $"Сообщение длиной {LastText.Length} не влезает в Telegram."
        );
        Assert.EndsWith("...", LastText);
    }

    /// <summary>
    /// Строка из одних пробелов отправлять незачем — таких сообщений в чате
    /// администратора быть не должно.
    /// </summary>
    [Fact]
    public async Task EmptyMessageIsNotSent()
    {
        var sent = CountSent();

        Logger(LogLevel.Trace).LogInformation("   ");

        await Task.Delay(200, TestContext.Current.CancellationToken);
        Assert.Equal(0, sent());
    }

    [Fact]
    public void FilterDecidesWhetherLevelIsEnabled()
    {
        var logger = new TelegramLogger(
            "MARS.Admin",
            Bot.Object,
            new TelegramLoggerOptions { ChatId = [42L], SourceName = "MARS" },
            (category, level) => level >= LogLevel.Error
        );

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Error));
    }

    /// <summary>
    /// Без фильтра разрешён любой уровень: иначе перезапись фильтра в
    /// конфигурации тихо отключала бы логирование.
    /// </summary>
    [Fact]
    public void WithoutFilterEveryLevelIsEnabled()
    {
        var logger = Logger(LogLevel.Trace);

        Assert.True(logger.IsEnabled(LogLevel.Trace));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void CategoryIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TelegramLogger(null!, Bot.Object, new TelegramLoggerOptions(), null)
        );
    }

    [Fact]
    public void NullFormatterIsRejected()
    {
        var logger = Logger(LogLevel.Trace);

        Assert.Throws<ArgumentNullException>(() =>
            logger.Log<object>(LogLevel.Error, new EventId(1), new object(), null, null!)
        );
    }

    /// <summary>
    /// Область видимости не поддерживается и возвращается null: ILogger обязан
    /// вернуть IDisposable, а null — допустимое значение.
    /// </summary>
    [Fact]
    public void ScopeIsNotSupported()
    {
        Assert.Null(Logger(LogLevel.Trace).BeginScope("scope"));
    }

    /// <summary>
    /// Наполненная очередь не должна ронять отправку: она ограничена, и если
    /// писатель не успевает, сообщение уходит мимо очереди, а не теряется.
    /// </summary>
    [Fact]
    public async Task QueueOverflowStillSendsMessage()
    {
        var bot = new Mock<ITelegramBotClient>(MockBehavior.Loose);
        using var sender = new TelegramLoggerSender(bot.Object, [42L]);

        for (var i = 0; i < 1100; i++)
        {
            sender.EnqueueMessage($"строка {i}");
        }

        await WaitUntil(() => bot.Invocations.Count > 0);
        Assert.Contains("строка", ((SendMessageRequest)bot.Invocations.Last().Arguments[0]).Text);
    }

    /// <summary>
    /// После Dispose очередь закрыта, поэтому сообщение уходит мимо неё и
    /// напрямую: иначе лог, написанный в момент остановки сервиса, пропал бы.
    /// </summary>
    [Fact]
    public async Task MessageAfterDisposeIsSentImmediately()
    {
        var bot = new Mock<ITelegramBotClient>(MockBehavior.Loose);
        var sender = new TelegramLoggerSender(bot.Object, [42L]);

        sender.Dispose();
        sender.EnqueueMessage("после остановки");

        await WaitUntil(() => bot.Invocations.Count > 0);
        Assert.Contains(
            bot.Invocations,
            invocation =>
                invocation.Arguments[0] is SendMessageRequest { Text: var text }
                && text.Contains("после остановки")
        );
    }

    /// <summary>
    /// Dispose дожидается отправки того, что уже в очереди: иначе последние
    /// логи перед остановкой сервиса молча пропали бы.
    /// </summary>
    [Fact]
    public async Task DisposeFlushesQueuedMessages()
    {
        var bot = new Mock<ITelegramBotClient>(MockBehavior.Loose);
        var sender = new TelegramLoggerSender(bot.Object, [42L]);

        sender.EnqueueMessage("перед остановкой");
        sender.Dispose();

        await WaitUntil(() => bot.Invocations.Count > 0);
        Assert.Contains(
            bot.Invocations,
            invocation =>
                invocation.Arguments[0] is SendMessageRequest { Text: var text }
                && text.Contains("перед остановкой")
        );
    }

    [Fact]
    public void ProviderReusesLoggerPerCategory()
    {
        using var provider = new TelegramLoggerProvider(
            Bot.Object,
            new TelegramLoggerOptions { ChatId = [42L], SourceName = "MARS" },
            null
        );

        Assert.Same(provider.CreateLogger("MARS.Admin"), provider.CreateLogger("MARS.Admin"));
        Assert.NotSame(provider.CreateLogger("A"), provider.CreateLogger("B"));
    }

    /// <summary>
    /// Без адресата и без имени источника провайдер не создаётся: логи ушли бы
    /// в никуда, а сервис стартовал бы с «работающим» логированием.
    /// </summary>
    [Fact]
    public void ProviderRequiresChatIdAndSourceName()
    {
        Assert.Throws<ArgumentException>(() =>
            new TelegramLoggerProvider(
                Bot.Object,
                new TelegramLoggerOptions { ChatId = [], SourceName = "MARS" },
                null
            )
        );

        Assert.Throws<ArgumentException>(() =>
            new TelegramLoggerProvider(
                Bot.Object,
                new TelegramLoggerOptions { ChatId = [42L], SourceName = "  " },
                null
            )
        );
    }

    private static TelegramLogger Logger(LogLevel minimumLevel) =>
        new(
            "MARS.Admin",
            Bot.Object,
            new TelegramLoggerOptions
            {
                ChatId = [42L],
                SourceName = "MARS",
                MinimumLevel = minimumLevel,
            },
            null
        );

    private static Func<int> CountSent()
    {
        Bot.Invocations.Clear();
        return () =>
            Bot.Invocations.Count(invocation => invocation.Arguments[0] is SendMessageRequest);
    }

    private static string LastText =>
        Bot
            .Invocations.LastOrDefault(invocation => invocation.Arguments[0] is SendMessageRequest)
            ?.Arguments[0]
            is SendMessageRequest request
            ? request.Text
            : string.Empty;

    private static long LastChatId =>
        Bot
            .Invocations.LastOrDefault(invocation => invocation.Arguments[0] is SendMessageRequest)
            ?.Arguments[0]
            is SendMessageRequest request
            ? request.ChatId.Identifier ?? 0
            : 0;

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 250 && !condition(); attempt++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }
}
