using MARS.MediaStorage.Presentation.Storage.Pages.Storage;

namespace MARS.MediaStorage.Tests;

public sealed class MediaStorageFileDisplayNameTests
{
    [Theory]
    [InlineData("miku.mp4", "miku")]
    [InlineData("archive.tar.gz", "archive.tar")]
    [InlineData("README", "README")]
    [InlineData(".gitignore", ".gitignore")]
    [InlineData("folder/miku.webm", "miku")]
    public void GetFileDisplayName_RemovesExtension(string fileName, string expected)
    {
        Assert.Equal(expected, IndexModel.GetFileDisplayName(fileName));
    }

    [Fact]
    public void GetTreeFileDisplayName_UsesDisplayName()
    {
        Assert.Equal("clip", IndexModel.GetTreeFileDisplayName("clip.mp4"));
    }
}
