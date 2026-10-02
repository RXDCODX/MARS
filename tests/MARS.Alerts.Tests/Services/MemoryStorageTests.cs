using MARS.Alerts.Services.PyroAlerts;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Файлы алертов в памяти: хранилище считает использования, чтобы удалить файл
/// только когда на него никто не ссылается. Проверяется именно счётчик — без
/// него файл, который ещё показывается в оверлее, исчез бы из-под зрителя.
///
/// Хранилище статическое, поэтому каждый тест начинает с очистки.
/// </summary>
[Collection(nameof(MemoryStorageTests))]
public class MemoryStorageTests : IDisposable
{
    private static readonly byte[] Content = [1, 2, 3, 4];

    public void Dispose() => MemoryStorage.ClearStorage();

    [Fact]
    public async Task AddedFileIsStoredUnderMemoryPrefix()
    {
        var path = await MemoryStorage.AddFileAsync("alert.mp3", Content);

        Assert.Equal("/memoryalert.mp3", path);
        Assert.True(MemoryStorage.FileExists("alert.mp3"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EmptyFileNameIsRejected(string? fileName)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.AddFileAsync(fileName!, Content)
        );
    }

    [Fact]
    public async Task SecondAddKeepsFileAndRaisesUseCount()
    {
        await MemoryStorage.AddFileAsync("alert.mp3", Content);
        await MemoryStorage.AddFileAsync("alert.mp3", Content);

        Assert.Equal(1, MemoryStorage.FileCount);

        // Два использования: после одного удаления файл обязан остаться.
        await MemoryStorage.DeleteFileAsync("alert.mp3");
        Assert.True(MemoryStorage.FileExists("alert.mp3"));

        await MemoryStorage.DeleteFileAsync("alert.mp3");
        Assert.False(MemoryStorage.FileExists("alert.mp3"));
    }

    [Fact]
    public async Task FileContentIsReturnedWithContentType()
    {
        await MemoryStorage.AddFileAsync("alert.mp3", Content);

        var (stream, contentType) = await MemoryStorage.GetFileStreamWithContentTypeAsync(
            "alert.mp3"
        );

        Assert.Equal("audio/mp3", contentType);
        Assert.Equal(Content, stream.ToArray());
    }

    [Theory]
    [InlineData("clip.webm", "video/webm")]
    [InlineData("picture.png", "image/png")]
    [InlineData("sticker.gif", "image/gif")]
    [InlineData("sticker.tgs", "application/octet-stream")]
    [InlineData("unknown.bin", "application/octet-stream")]
    public async Task ContentTypeFollowsFileKind(string fileName, string expected)
    {
        await MemoryStorage.AddFileAsync(fileName, Content);

        var (_, contentType) = await MemoryStorage.GetFileStreamWithContentTypeAsync(fileName);

        Assert.Equal(expected, contentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ReadingWithoutNameIsRejected(string? fileName)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.GetFileStreamWithContentTypeAsync(fileName!)
        );
    }

    [Fact]
    public async Task ReadingMissingFileFails()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            MemoryStorage.GetFileStreamWithContentTypeAsync("нет-такого.mp3")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DeletingWithoutNameIsRejected(string? fileName)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            MemoryStorage.DeleteFileAsync(fileName!)
        );
    }

    [Fact]
    public void ExistsRequiresName()
    {
        Assert.False(MemoryStorage.FileExists(string.Empty));
        Assert.False(MemoryStorage.FileExists("   "));
        Assert.False(MemoryStorage.FileExists("не-существует.mp3"));
    }

    [Fact]
    public async Task AllNamesAreListed()
    {
        await MemoryStorage.AddFileAsync("one.mp3", Content);
        await MemoryStorage.AddFileAsync("two.mp3", Content);

        var names = await MemoryStorage.GetAllFileNamesAsync();

        Assert.Equal(2, names.Length);
        Assert.Contains("one.mp3", names);
        Assert.Contains("two.mp3", names);
    }

    [Fact]
    public async Task ClearEmptiesStorage()
    {
        await MemoryStorage.AddFileAsync("one.mp3", Content);

        await MemoryStorage.ClearStorageAsync();

        Assert.Equal(0, MemoryStorage.FileCount);
        Assert.Equal(0UL, MemoryStorage.StorageSize);
    }

    [Fact]
    public async Task StorageSizeSumsContentLength()
    {
        await MemoryStorage.AddFileAsync("one.mp3", Content);
        await MemoryStorage.AddFileAsync("two.mp3", [9, 9]);

        Assert.Equal(6UL, MemoryStorage.StorageSize);
    }
}
