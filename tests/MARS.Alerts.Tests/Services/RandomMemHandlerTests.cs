using MARS.Alerts.Services.PyroAlerts;
using MARS.Alerts.Services.Twitch.Rewards;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Сбор мемов из папки Telegram.
///
/// Файлы из личных сообщений боту складываются в папку и потом показываются всему
/// каналу. Проверяется, что сообщение без вложения ничего не скачивает, и что путь
/// внутри папки не позволяет записать файл в другое место.
/// </summary>
public class RandomMemHandlerTests : IDisposable
{
    private readonly string _webRoot = Path.Combine(
        Path.GetTempPath(),
        "mars-random-mem",
        Guid.NewGuid().ToString("N")
    );

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, true);
        }
    }

    /// <summary>
    /// Сообщение без файла игнорируется: нечего складывать в папку, а попытка
    /// разобрать путь выдала бы ошибку в журнале на каждом текстовом сообщении.
    /// </summary>
    [Fact]
    public async Task MessageWithoutFileIsIgnored()
    {
        await Create().HandMessage(Mock.Of<ITelegramBotClient>(), TextMessage());

        Assert.Empty(
            Directory.Exists(Path.Combine(_webRoot, "Alerts", "random_meme"))
                ? Directory.GetFiles(Path.Combine(_webRoot, "Alerts", "random_meme"))
                : []
        );
    }

    /// <summary>
    /// Выключенный обработчик не трогает ничего: иначе выключатель в конфигурации
    /// не работал бы.
    /// </summary>
    [Fact]
    public async Task InactiveHandlerIgnoresMessage()
    {
        var handler = Create();
        handler.IsServiceActive = false;

        await handler.HandMessage(Mock.Of<ITelegramBotClient>(), TextMessage());
    }

    /// <summary>
    /// Апдейт не от чата игнорируется: обработчик реагирует только на сообщения.
    /// </summary>
    [Fact]
    public async Task NonMessageUpdateIsIgnored()
    {
        await Create()
            .HandMessage(
                Mock.Of<ITelegramBotClient>(),
                new Update { Id = 1, Message = TextMessage().Message! }
            );
    }

    private RandomMemHandler Create()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "Alerts", "random_meme"));

        return new RandomMemHandler(
            new FakeEnvironment(_webRoot),
            new PyroAlertsHelper(NullLogger<PyroAlertsHelper>.Instance),
            NullLogger<RandomMemHandler>.Instance
        );
    }

    private static Update TextMessage() =>
        new()
        {
            Id = 1,
            Message = new Message
            {
                Id = 1,
                Date = DateTime.Now,
                Text = "привет",
                Chat = new Chat { Id = 42, Type = ChatType.Private },
            },
        };

    private sealed class FakeEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MARS.Alerts.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Test";
    }
}
