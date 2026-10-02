using System.Text;
using MARS.Telegram.Services.MemoryStorageService;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Хранилище файлов в памяти, из которого Telegram отдаёт картинки по
/// виртуальным путям <c>/memory/имя</c>.
///
/// Хранилище статическое и общее для процесса, поэтому каждый тест очищает его
/// и в начале, и в конце: иначе тесты видели бы файлы друг друга и счётчики
/// использования сбивались бы.
///
/// Главное проверяемое правило — счётчик ссылок. Файл удаляется только когда
/// счётчик дошёл до нуля, иначе второй вызов удаления стёр бы файл, на который
/// ещё ссылается отправитель.
/// </summary>
/// <summary>
/// Хранилище в памяти статическое и общее для всего процесса, поэтому все
/// тесты, работающие с ним, обязаны идти в одной коллекции: иначе они идут
/// параллельно и видят файлы друг друга (тест очищает хранилище — и чужой тест
/// падает на отсутствии своего файла).
/// </summary>
[Collection("MemoryStorage")]
public class MemoryStorageTests : IDisposable
{
    private static readonly byte[] Content = Encoding.UTF8.GetBytes("содержимое");

    public MemoryStorageTests() => MemoryStorage.ClearStorage();

    public void Dispose() => MemoryStorage.ClearStorage();

    /// <summary>
    /// Ключ хранилища — путь из оповещения, он начинается со «/», поэтому
    /// префикс <c>/memory</c> даёт ссылку вида <c>/memory/img/12345.png</c>.
    /// Именно такую ссылку и возвращает сервис вызывающему коду.
    /// </summary>
    [Fact]
    public async Task AddedFileIsReturnedAsMemoryPath()
    {
        var path = await MemoryStorage.AddFileAsync("/img/картинка.png", Content);

        Assert.Equal("/memory/img/картинка.png", path);
        Assert.True(MemoryStorage.FileExists("/img/картинка.png"));
    }

    [Fact]
    public async Task AddingSameFileTwiceKeepsContent()
    {
        await MemoryStorage.AddFileAsync("картинка.png", Content);

        await MemoryStorage.AddFileAsync("картинка.png", [1, 2, 3]);

        var (stream, _) = await MemoryStorage.GetFileStreamWithContentTypeAsync("картинка.png");
        Assert.Equal(Content.Length, stream.Length);
    }

    [Fact]
    public async Task ContentTypeIsDerivedFromExtension()
    {
        await MemoryStorage.AddFileAsync("кадр.mp4", Content);

        var (_, contentType) = await MemoryStorage.GetFileStreamWithContentTypeAsync("кадр.mp4");

        Assert.Equal("video/mp4", contentType);
    }

    [Theory]
    [InlineData("файл.jpg", "image/jpeg")]
    [InlineData("файл.JPEG", "image/jpeg")]
    [InlineData("файл.webp", "image/webp")]
    [InlineData("файл.webm", "video/webm")]
    [InlineData("файл.bin", "application/octet-stream")]
    [InlineData("без расширения", "application/octet-stream")]
    public async Task UnknownExtensionFallsBackToOctetStream(string fileName, string expected)
    {
        await MemoryStorage.AddFileAsync(fileName, Content);

        var (_, contentType) = await MemoryStorage.GetFileStreamWithContentTypeAsync(fileName);

        Assert.Equal(expected, contentType);
    }

    [Fact]
    public async Task FileIsDeletedOnlyAfterLastReference()
    {
        await MemoryStorage.AddFileAsync("картинка.png", Content);
        await MemoryStorage.AddFileAsync("картинка.png", Content);

        await MemoryStorage.DeleteFileAsync("картинка.png");

        Assert.True(MemoryStorage.FileExists("картинка.png"));
    }

    [Fact]
    public async Task FileIsDeletedWhenReferencesAreGone()
    {
        await MemoryStorage.AddFileAsync("картинка.png", Content);
        await MemoryStorage.AddFileAsync("картинка.png", Content);

        await MemoryStorage.DeleteFileAsync("картинка.png");
        await MemoryStorage.DeleteFileAsync("картинка.png");

        Assert.False(MemoryStorage.FileExists("картинка.png"));
    }

    [Fact]
    public async Task MissingFileCannotBeRead()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            MemoryStorage.GetFileStreamWithContentTypeAsync("нет-такого.png")
        );
    }

    [Fact]
    public async Task StoredContentIsReturnedAsIs()
    {
        await MemoryStorage.AddFileAsync("картинка.png", Content);

        var (stream, _) = await MemoryStorage.GetFileStreamWithContentTypeAsync("картинка.png");

        Assert.Equal(Content, stream.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankNameIsRejected(string? fileName)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.AddFileAsync(fileName!, Content)
        );
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.GetFileStreamWithContentTypeAsync(fileName!)
        );
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.DeleteFileAsync(fileName!)
        );
    }

    [Fact]
    public async Task AllFileNamesAreListed()
    {
        await MemoryStorage.AddFileAsync("первая.png", Content);
        await MemoryStorage.AddFileAsync("вторая.png", Content);

        var names = await MemoryStorage.GetAllFileNamesAsync();

        Assert.Equal(["вторая.png", "первая.png"], names.Order());
    }

    [Fact]
    public async Task StorageIsCountedAndMeasured()
    {
        await MemoryStorage.AddFileAsync("первая.png", Content);
        await MemoryStorage.AddFileAsync("вторая.png", [1]);

        Assert.Equal(2, MemoryStorage.FileCount);
        Assert.Equal((ulong)(Content.Length + 1), MemoryStorage.StorageSize);
    }

    [Fact]
    public async Task ClearingRemovesEverything()
    {
        await MemoryStorage.AddFileAsync("первая.png", Content);

        await MemoryStorage.ClearStorageAsync();

        Assert.Equal(0, MemoryStorage.FileCount);
    }
}
