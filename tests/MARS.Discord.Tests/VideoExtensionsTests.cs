using MARS.Discord.Services.Media;

namespace MARS.Discord.Tests;

/// <summary>
/// Классификация вложений по расширению.
/// </summary>
public class VideoExtensionsTests
{
    [Theory]
    [InlineData("clip.mp4")]
    [InlineData("clip.MKV")]
    [InlineData("clip.mov")]
    [InlineData("clip.webm")]
    [InlineData("clip.avi")]
    [InlineData("clip.wmv")]
    [InlineData("clip.mpeg")]
    [InlineData("clip.mpg")]
    public void IsVideoFile_AcceptsEveryVideoExtension(string fileName)
    {
        Assert.True(VideoExtensions.IsVideoFile(fileName));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.PNG")]
    [InlineData("photo.webp")]
    [InlineData("photo.tiff")]
    [InlineData("track.mp3")]
    [InlineData("notes.txt")]
    [InlineData("noextension")]
    public void IsVideoFile_RejectsEverythingElse(string fileName)
    {
        Assert.False(VideoExtensions.IsVideoFile(fileName));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    [InlineData("photo.gif")]
    [InlineData("photo.webp")]
    [InlineData("photo.bmp")]
    [InlineData("photo.tiff")]
    [InlineData("photo.tif")]
    public void IsImageFile_AcceptsEveryImageExtension(string fileName)
    {
        Assert.True(VideoExtensions.IsImageFile(fileName));
    }

    [Fact]
    public void IsImageFile_RejectsGiflessLookalike()
    {
        Assert.False(VideoExtensions.IsImageFile("clip.mp4"));
        Assert.False(VideoExtensions.IsImageFile("noextension"));
    }

    [Theory]
    [InlineData("track.mp3")]
    [InlineData("track.ogg")]
    [InlineData("track.aac")]
    [InlineData("track.flac")]
    [InlineData("track.wav")]
    [InlineData("track.opus")]
    [InlineData("track.weba")]
    public void IsAudioFile_AcceptsEveryAudioExtension(string fileName)
    {
        Assert.True(VideoExtensions.IsAudioFile(fileName));
    }

    /// <summary>
    /// Классификация не должна бросать на пустом имени или имени без точки:
    /// вызывающая сторона приходит с пользовательским вводом, а не с
    /// гарантированным именем файла.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    public void Classification_IsSafeForDegenerateNames(string fileName)
    {
        Assert.False(VideoExtensions.IsImageFile(fileName));
        Assert.False(VideoExtensions.IsVideoFile(fileName));
        Assert.False(VideoExtensions.IsAudioFile(fileName));
    }
}
