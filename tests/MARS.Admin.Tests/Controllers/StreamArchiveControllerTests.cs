using System.Reflection;
using MARS.Admin.Controllers;

namespace MARS.Admin.Tests.Controllers;

/// <summary>
/// Проверка приватного метода <see cref="StreamArchiveController.IsVideoFile"/>
/// через рефлексию: метод не имеет публичной обёртки, поэтому тестируется
/// напрямую.
/// </summary>
public class StreamArchiveControllerTests
{
    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("video.avi", true)]
    [InlineData("video.mkv", true)]
    [InlineData("video.mov", true)]
    [InlineData("video.wmv", true)]
    [InlineData("video.flv", true)]
    [InlineData("video.webm", true)]
    [InlineData("video.m4v", true)]
    [InlineData("video.3gp", true)]
    [InlineData("document.txt", false)]
    [InlineData("image.png", false)]
    [InlineData("archive.zip", false)]
    [InlineData("", false)]
    public void IsVideoFileRecognizesVideoExtensions(string filePath, bool expected)
    {
        var method = typeof(StreamArchiveController).GetMethod(
            "IsVideoFile",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.NotNull(method);

        var result = (bool)method.Invoke(null, [filePath])!;

        Assert.Equal(expected, result);
    }
}
