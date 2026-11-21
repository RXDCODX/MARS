using MARS.Shared.Contracts.Media;
using MARS.Shared.Contracts.Models.Media;

namespace MARS.Shared.Tests.Media;

/// <summary>
/// Общая таблица расширений — единственный источник правды для типов медиа и MIME.
/// </summary>
public class MediaFileTypesTests
{
    [Theory]
    [InlineData("alert.tgs", MediaType.TelegramSticker, "application/octet-stream")]
    [InlineData("sound.OGG", MediaType.Audio, "audio/ogg")]
    [InlineData("movie.mkv", MediaType.None, "video/x-matroska")]
    [InlineData("/alerts/photo.JPEG", MediaType.Image, "image/jpeg")]
    [InlineData("meme.gif", MediaType.Gif, "image/gif")]
    public void Known_extension_has_type_and_content_type(
        string fileName,
        MediaType expectedType,
        string expectedContentType
    )
    {
        Assert.Equal(expectedType, MediaFileTypes.GetMediaType(fileName));
        Assert.Equal(expectedContentType, MediaFileTypes.GetContentType(fileName));
    }

    [Fact]
    public void Unknown_extension_uses_safe_defaults()
    {
        Assert.Equal(MediaType.None, MediaFileTypes.GetMediaType("archive.unknown"));
        Assert.Equal("application/octet-stream", MediaFileTypes.GetContentType("archive.unknown"));
        Assert.Equal(MediaType.None, MediaFileTypes.GetMediaType(null));
    }
}
