using MARS.MediaStorage.Entities;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Tests.Entities;

/// <summary>
/// Копия записи алерта.
///
/// Копирование проверяется на независимости: хранилище правит файл записи при
/// конвертации, и общий объект с исходной записью испортил бы оба.
/// </summary>
public class ApiMediaInfoTests
{
    [Fact]
    public void CopyKeepsFields()
    {
        var source = Alert();
        var copy = new ApiMediaInfo(source);

        Assert.Equal(source.Id, copy.Id);
        Assert.Equal(source.FileInfo.FileName, copy.FileInfo.FileName);
        Assert.Equal(source.MetaInfo.DisplayName, copy.MetaInfo.DisplayName);
    }

    /// <summary>
    /// Копия из обычного <see cref="MediaInfo"/> используется приёмом файла: поля
    /// переносятся, а сам объект остаётся независимым.
    /// </summary>
    [Fact]
    public void CopyFromMediaInfoKeepsFields()
    {
        var source = new MediaInfo
        {
            TextInfo = new MediaTextInfo { Text = "текст" },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Audio,
                FilePath = "memory/a.mp3",
                FileName = "a",
                Extension = ".mp3",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "аяка" },
            StylesInfo = new MediaStylesInfo(),
        };

        var copy = new ApiMediaInfo(source);

        Assert.Equal(source.Id, copy.Id);
        Assert.Equal("аяка", copy.MetaInfo.DisplayName);
    }

    /// <summary>
    /// Пустой конструктор нужен сериализатору: без него десериализация записи
    /// хранилища падала бы.
    /// </summary>
    [Fact]
    public void EmptyConstructorExists()
    {
        // Сериализатору нужен конструктор без обязательных полей: иначе запись из
        // базы не десериализуется.
        var alert = new ApiMediaInfo
        {
            TextInfo = new MediaTextInfo { Text = "текст" },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Audio,
                FilePath = "memory/a.mp3",
                FileName = "a",
                Extension = ".mp3",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "аяка" },
            StylesInfo = new MediaStylesInfo(),
        };

        Assert.Equal("аяка", alert.MetaInfo.DisplayName);
    }

    private static ApiMediaInfo Alert() =>
        new()
        {
            TextInfo = new MediaTextInfo { Text = "текст" },
            FileInfo = new MediaFileInfo
            {
                Type = MediaType.Audio,
                FilePath = "memory/a.mp3",
                FileName = "a",
                Extension = ".mp3",
            },
            PositionInfo = new MediaPositionInfo(),
            MetaInfo = new MediaMetaInfo { DisplayName = "аяка" },
            StylesInfo = new MediaStylesInfo(),
        };
}
