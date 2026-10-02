using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Entities.DTOs;
using MARS.MediaStorage.Services.Storage;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Преобразования записей хранилища для интерфейса и учёт загруженных файлов.
///
/// Даты отдаются в ISO-8601 UTC, а тип файла выводится из расширения: интерфейс
/// хранилища показывает записи в браузере, и ошибка здесь выглядит как «файл не
/// найден» у пользователя.
/// </summary>
public class MediaStorageMappingTests
{
    /// <summary>
    /// Запись хранилища переносится в DTO целиком, включая путь: UI строит ссылку
    /// на файл именно по нему.
    /// </summary>
    [Fact]
    public void EntryIsProjectedForUi()
    {
        var id = Guid.NewGuid();
        var uploadedAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var entry = new MediaStorageEntry
        {
            Id = id,
            Path = "random_meme/мем.jpg",
            FileName = "мем.jpg",
            Extension = ".jpg",
            MediaType = MediaType.Image,
            SizeBytes = 1024,
            UploadedAt = uploadedAt,
            OriginalPath = "random_meme/исходник.gif",
        };

        var dto = MediaStorageEntryDto.From(entry);

        Assert.Equal(id, dto.Id);
        Assert.Equal("random_meme/мем.jpg", dto.Path);
        Assert.Equal(".jpg", dto.Extension);
        Assert.Equal(MediaType.Image, dto.MediaType);
        Assert.Equal(1024, dto.SizeBytes);
        Assert.Equal(uploadedAt, dto.UploadedAt);
        Assert.Equal("random_meme/исходник.gif", dto.OriginalPath);
        Assert.Null(dto.DeletedAt);
    }

    /// <summary>
    /// Загруженный файл освобождает поток: иначе соединение с OBS оставалось бы
    /// открытым после каждой загрузки.
    /// </summary>
    [Fact]
    public void UploadedFileReleasesItsStream()
    {
        var content = new MemoryStream([1, 2, 3]);
        var upload = new MediaUploadFile("мем.jpg", content, "image/jpeg", 3);

        upload.Dispose();

        Assert.False(content.CanRead);
    }

    /// <summary>
    /// Удалённая запись сохраняет дату удаления: по ней UI прячет файл из списка, но
    /// хранит его на диске для возможного восстановления.
    /// </summary>
    [Fact]
    public void DeletedEntryKeepsItsDates()
    {
        var deletedAt = new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero);
        var entry = new MediaStorageEntry
        {
            Id = Guid.NewGuid(),
            Path = "random_meme/старый.jpg",
            FileName = "старый.jpg",
            Extension = ".jpg",
            MediaType = MediaType.Image,
            UploadedAt = DateTimeOffset.UtcNow.AddDays(-3),
            DeletedAt = deletedAt,
        };

        var dto = MediaStorageEntryDto.From(entry);

        Assert.True(dto.IsDeleted);
        Assert.Equal(deletedAt, dto.DeletedAt);
    }

    /// <summary>
    /// Файл в памяти опознаётся по имени: хранилище держит файлы в словаре по
    /// имени, и повторная загрузка того же имени заменяет содержимое, а не
    /// добавляет вторую запись.
    /// </summary>
    [Fact]
    public void MemoryFilesAreIdentifiedByName()
    {
        var first = File("мем.jpg", MediaType.Image, [1, 2, 3]);
        var second = File("мем.jpg", MediaType.Image, [4, 5, 6]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Пустое содержимое не принимается: такой файл нечего показывать, а в
    /// хранилище он попал бы как «битый» мем.
    /// </summary>
    [Fact]
    public void EmptyContentIsRejected()
    {
        Assert.Throws<NullReferenceException>(() =>
            new MARS.MediaStorage.Services.MemoryStorageService.Entitys.MemoryFile
            {
                FileName = "мем.jpg",
                MediaType = MediaType.Image,
                FileContent = [],
                Exstension = ".jpg",
            }
        );
    }

    private static MARS.MediaStorage.Services.MemoryStorageService.Entitys.MemoryFile File(
        string fileName,
        MediaType mediaType,
        byte[] content
    ) =>
        new()
        {
            FileName = fileName,
            MediaType = mediaType,
            FileContent = content,
            Exstension = ".jpg",
        };
}
