using System.Text;
using MARS.Telegram.Application.Clipboard;

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

    public MemoryStorageTests() => ClipboardFileScratch.ClearStorage();

    public void Dispose() => ClipboardFileScratch.ClearStorage();

    /// <summary>
    /// Ключ хранилища — путь из оповещения, он начинается со «/», поэтому
    /// префикс <c>/memory</c> даёт ссылку вида <c>/memory/img/12345.png</c>.
    /// Именно такую ссылку и возвращает сервис вызывающему коду.
    /// </summary>
    [Fact]
    public async Task AddedFileIsReturnedAsMemoryPath()
    {
        var path = await ClipboardFileScratch.AddFileAsync("/img/картинка.png", Content);

        Assert.Equal("/memory/img/картинка.png", path);
        Assert.True(ClipboardFileScratch.FileExists("/img/картинка.png"));
    }

    [Fact]
    public async Task AddingSameFileTwiceKeepsContent()
    {
        await ClipboardFileScratch.AddFileAsync("картинка.png", Content);

        await ClipboardFileScratch.AddFileAsync("картинка.png", [1, 2, 3]);

        var (stream, _) = await ClipboardFileScratch.GetFileStreamWithContentTypeAsync(
            "картинка.png"
        );
        Assert.Equal(Content.Length, stream.Length);
    }

    [Fact]
    public async Task ContentTypeIsDerivedFromExtension()
    {
        await ClipboardFileScratch.AddFileAsync("кадр.mp4", Content);

        var (_, contentType) = await ClipboardFileScratch.GetFileStreamWithContentTypeAsync(
            "кадр.mp4"
        );

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
        await ClipboardFileScratch.AddFileAsync(fileName, Content);

        var (_, contentType) = await ClipboardFileScratch.GetFileStreamWithContentTypeAsync(
            fileName
        );

        Assert.Equal(expected, contentType);
    }

    [Fact]
    public async Task FileIsDeletedOnlyAfterLastReference()
    {
        await ClipboardFileScratch.AddFileAsync("картинка.png", Content);
        await ClipboardFileScratch.AddFileAsync("картинка.png", Content);

        await ClipboardFileScratch.DeleteFileAsync("картинка.png");

        Assert.True(ClipboardFileScratch.FileExists("картинка.png"));
    }

    [Fact]
    public async Task FileIsDeletedWhenReferencesAreGone()
    {
        await ClipboardFileScratch.AddFileAsync("картинка.png", Content);
        await ClipboardFileScratch.AddFileAsync("картинка.png", Content);

        await ClipboardFileScratch.DeleteFileAsync("картинка.png");
        await ClipboardFileScratch.DeleteFileAsync("картинка.png");

        Assert.False(ClipboardFileScratch.FileExists("картинка.png"));
    }

    [Fact]
    public async Task MissingFileCannotBeRead()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            ClipboardFileScratch.GetFileStreamWithContentTypeAsync("нет-такого.png")
        );
    }

    [Fact]
    public async Task StoredContentIsReturnedAsIs()
    {
        await ClipboardFileScratch.AddFileAsync("картинка.png", Content);

        var (stream, _) = await ClipboardFileScratch.GetFileStreamWithContentTypeAsync(
            "картинка.png"
        );

        Assert.Equal(Content, stream.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankNameIsRejected(string? fileName)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ClipboardFileScratch.AddFileAsync(fileName!, Content)
        );
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ClipboardFileScratch.GetFileStreamWithContentTypeAsync(fileName!)
        );
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ClipboardFileScratch.DeleteFileAsync(fileName!)
        );
    }

    [Fact]
    public async Task AllFileNamesAreListed()
    {
        await ClipboardFileScratch.AddFileAsync("первая.png", Content);
        await ClipboardFileScratch.AddFileAsync("вторая.png", Content);

        var names = await ClipboardFileScratch.GetAllFileNamesAsync();

        Assert.Equal(["вторая.png", "первая.png"], names.Order());
    }

    [Fact]
    public async Task StorageIsCountedAndMeasured()
    {
        await ClipboardFileScratch.AddFileAsync("первая.png", Content);
        await ClipboardFileScratch.AddFileAsync("вторая.png", [1]);

        Assert.Equal(2, ClipboardFileScratch.FileCount);
        Assert.Equal((ulong)(Content.Length + 1), ClipboardFileScratch.StorageSize);
    }

    [Fact]
    public async Task ClearingRemovesEverything()
    {
        await ClipboardFileScratch.AddFileAsync("первая.png", Content);

        await ClipboardFileScratch.ClearStorageAsync();

        Assert.Equal(0, ClipboardFileScratch.FileCount);
    }
}
