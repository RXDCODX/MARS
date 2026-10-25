using MARS.Telegram.Services.ClipboardCopy;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Файлы, переданные в буфер обмена: имена файлов в памяти и время, когда их
/// собрали.
/// </summary>
/// <remarks>
/// Список нужен для того, чтобы сервис знал, какие файлы ещё живут: имена
/// хранятся в памяти, а не на диске, и после перезапуска сервиса их не
/// восстановить. Поэтому время создания и есть признак «файл ещё в памяти».
/// </remarks>
public class ClipboardRequestFilesTests
{
    [Fact]
    public void Хранит_Имена_Файлов_И_Время_Создания()
    {
        var createdAt = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        var files = new ClipboardRequestFiles(new[] { "first.png", "second.png" }, createdAt);

        Assert.Equal(new[] { "first.png", "second.png" }, files.MemoryFileNames);
        Assert.Equal(createdAt, files.CreatedAt);
    }

    /// <summary>
    /// Пустой список файлов — законное состояние: копировать было нечего, и
    /// обслуживание не должно считать это ошибкой.
    /// </summary>
    [Fact]
    public void Пустой_Список_Файлов_Допустим()
    {
        var files = new ClipboardRequestFiles([], DateTime.UtcNow);

        Assert.Empty(files.MemoryFileNames);
    }
}
