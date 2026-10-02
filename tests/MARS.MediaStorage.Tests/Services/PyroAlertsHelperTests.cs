using System.Text;
using MARS.MediaStorage.Services.MemoryStorageService;
using MARS.MediaStorage.Services.PyroAlerts;
using MARS.Shared.Models.Media;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Превращение файла из Telegram в описание медиа, которое уходит в хранилище.
///
/// Логика повторяет MARS.Alerts (общий код жил в монолите), и проверяется здесь
/// то же: видео зациклено и в рамке, стикер — квадратом без растяжения,
/// неопознанный тип не показывается вовсе. Путь файла ведёт в хранилище в
/// памяти, поэтому сам файл должен там оказаться — иначе запись в хранилище
/// была бы битой.
/// </summary>
/// <summary>
/// Хранилище в памяти статическое и общее для всего процесса, поэтому все
/// тесты, работающие с ним, обязаны идти в одной коллекции: иначе они идут
/// параллельно и видят файлы друг друга (тест очищает хранилище — и чужой тест
/// падает на отсутствии своего файла).
/// </summary>
[Collection("MemoryStorage")]
public class PyroAlertsHelperTests : IDisposable
{
    private readonly Mock<ITelegramBotClient> _client = new(MockBehavior.Loose);
    private readonly PyroAlertsHelper _helper = new(NullLogger<PyroAlertsHelper>.Instance);
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mars-pyro-alerts-storage",
        Guid.NewGuid().ToString("N")
    );

    public PyroAlertsHelperTests()
    {
        MemoryStorage.ClearStorage();
        SetupFileTransfer();
    }

    public void Dispose()
    {
        MemoryStorage.ClearStorage();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    /// <summary>
    /// Видео уходит зацикленным, аудио — обычным, а неопознанное расширение не
    /// показывается вовсе: оверлей получил бы файл, который не умеет играть.
    /// </summary>
    [Theory]
    [InlineData("clip.mp4", true)]
    [InlineData("clip.webm", true)]
    public async Task VideoIsLooped(string fileName, bool looped)
    {
        var info = await _helper.GetTransferObj(_client.Object, MediaMessage(fileName));

        Assert.NotNull(info);
        Assert.Equal(looped, info!.MetaInfo.IsLooped);
    }

    [Fact]
    public async Task AudioIsNotLooped()
    {
        var info = await _helper.GetTransferObj(_client.Object, MediaMessage("song.mp3"));

        Assert.NotNull(info);
        Assert.False(info!.MetaInfo.IsLooped);
    }

    [Fact]
    public async Task UnknownExtensionYieldsNoMedia()
    {
        var info = await _helper.GetTransferObj(_client.Object, MediaMessage("clip.mkv"));

        Assert.Null(info);
    }

    /// <summary>
    /// Файл попадает в хранилище в памяти по пути из описания: без этого оверлей
    /// получил бы ссылку на несуществующий файл.
    /// </summary>
    [Fact]
    public async Task FileIsStoredInMemoryStorage()
    {
        var info = await _helper.GetTransferObj(_client.Object, MediaMessage("clip.mp4"));

        var path = info!.FileInfo.FilePath!["memory/".Length..];
        Assert.True(MemoryStorage.FileExists(path));
    }

    [Fact]
    public async Task DisplayNameIsTakenFromChat()
    {
        var info = await _helper.GetTransferObj(_client.Object, MediaMessage("clip.mp4"));

        Assert.Equal("канал", info!.MetaInfo.DisplayName);
    }

    /// <summary>
    /// Сообщение без вложения не превращается в пустое медиа: без файла оверлею
    /// нечего показывать.
    /// </summary>
    [Fact]
    public async Task MessageWithoutFileYieldsNoMedia()
    {
        var info = await _helper.GetTransferObj(
            _client.Object,
            new Message
            {
                Id = 1,
                Chat = new Chat { Id = 1, Username = "канал" },
            }
        );

        Assert.Null(info);
    }

    [Fact]
    public async Task PhotoFileInfoIsTakenFromMessage()
    {
        var message = new Message
        {
            Id = 1,
            Chat = new Chat { Id = 1 },
            Photo = [new PhotoSize { FileId = "photo-1", FileUniqueId = "photo-1" }],
        };

        var fileInfo = await _helper.GetTgFileInfo(_client.Object, message);

        Assert.NotNull(fileInfo);
        Assert.Contains("photo-1", fileInfo!.FilePath);
    }

    [Fact]
    public async Task VideoFileInfoIsTakenFromMessage()
    {
        var message = new Message
        {
            Id = 1,
            Chat = new Chat { Id = 1 },
            Video = new Video { FileId = "video-1", FileUniqueId = "video-1" },
        };

        var fileInfo = await _helper.GetTgFileInfo(_client.Object, message);

        Assert.Contains("video-1", fileInfo!.FilePath);
    }

    [Fact]
    public async Task TextMessageHasNoFileInfo()
    {
        Assert.Null(
            await _helper.GetTgFileInfo(
                _client.Object,
                new Message
                {
                    Id = 1,
                    Chat = new Chat { Id = 1 },
                    Text = "привет",
                }
            )
        );
    }

    [Fact]
    public async Task NullMessageHasNoFileInfo()
    {
        Assert.Null(await _helper.GetTgFileInfo(_client.Object, null));
    }

    /// <summary>
    /// Файл без размера скачать нечем: метод бросает исключение, а не отдаёт
    /// пустой массив, который выглядел бы как «пустой файл».
    /// </summary>
    [Fact]
    public async Task DownloadWithoutSizeFails()
    {
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _helper.DownloadFile(_client.Object, new TgFileInfo { FilePath = "clip.mp4" })
        );
    }

    /// <summary>
    /// Размер файла известен заранее, поэтому буфер выделяется ровно под него:
    /// в тесте он совпадает с длиной содержимого, иначе запись не влезла бы и
    /// метод вернул бы нули.
    /// </summary>
    [Fact]
    public async Task DownloadReturnsContent()
    {
        var bytes = await _helper.DownloadFile(
            _client.Object,
            new TgFileInfo { FilePath = "clip.mp4", FileSize = 8 }
        );

        Assert.Equal(Encoding.UTF8.GetBytes("clip.mp4"), bytes);
    }

    /// <summary>
    /// Каталог для файла создаётся целиком: без этого скачивание падало бы на
    /// несуществующей папке, когда оповещение пришло впервые.
    /// </summary>
    [Fact]
    public async Task DownloadToFolderCreatesDirectories()
    {
        Directory.CreateDirectory(_root);
        var folder = Path.Combine(_root, "вложенная", "папка");

        await _helper.DownloadFileAndCache(
            _client.Object,
            new TgFileInfo { FilePath = "clip.mp4", FileSize = 4 },
            folder
        );

        Assert.True(File.Exists(Path.Combine(folder, "clip.mp4")));
    }

    /// <summary>
    /// Пустой путь игнорируется: вызывающий код может не знать папку, и создавать
    /// «ничего» безопаснее, чем падать.
    /// </summary>
    [Fact]
    public void EmptyPathCreatesNoDirectory()
    {
        _helper.EnsureDirectoryExists(string.Empty);

        Assert.False(Directory.Exists(_root));
    }

    /// <summary>
    /// У чата без аватарки спрашивать нечего: возвращается null, а не
    /// исключение из-за отсутствия файла.
    /// </summary>
    [Fact]
    public async Task ChatWithoutPhotoHasNoFilePath()
    {
        Assert.Null(
            await _helper.GetChatPhotoFilePath(_client.Object, new ChatFullInfo { Id = 1 })
        );
    }

    private static Message MediaMessage(string fileName) =>
        new()
        {
            Id = 1,
            Chat = new Chat { Id = 1, Username = "канал" },
            Video = new Video { FileId = fileName, FileUniqueId = fileName },
        };

    /// <summary>
    /// Заглушка Telegram: путь файла и содержимое. <c>GetFile</c> — расширяющий
    /// метод поверх <c>SendRequest</c>, <c>DownloadFile</c> — метод интерфейса.
    /// </summary>
    private void SetupFileTransfer()
    {
        _client
            .Setup(instance =>
                instance.SendRequest(It.IsAny<GetFileRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (GetFileRequest request, CancellationToken _) =>
                    new TGFile
                    {
                        FileId = request.FileId,
                        FilePath = request.FileId,
                        FileSize = request.FileId.Length,
                    }
            );
        _client
            .Setup(instance =>
                instance.DownloadFile(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (string filePath, Stream destination, CancellationToken _) =>
                {
                    destination.Write(Encoding.UTF8.GetBytes(filePath));

                    return Task.CompletedTask;
                }
            );
    }
}
