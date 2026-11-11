namespace MARS.MediaStorage.Tests;

public class MediaPathTests
{
    [Theory]
    [InlineData("Alerts\\zvik\\cat.mp3", "Alerts/zvik/cat.mp3")]
    [InlineData("Alerts/zvik/cat.mp3", "Alerts/zvik/cat.mp3")]
    [InlineData("/Alerts/zvik/cat.mp3", "Alerts/zvik/cat.mp3")]
    [InlineData("Alerts//zvik///cat.mp3", "Alerts/zvik/cat.mp3")]
    [InlineData("Alerts\\zvik/cat.mp3", "Alerts/zvik/cat.mp3")]
    public void Normalize_ProducesForwardSlashes(string input, string expected)
    {
        var result = MediaPath.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_TrimsTrailingSlash()
    {
        var result = MediaPath.Normalize("Alerts/zvik/");

        Assert.Equal("Alerts/zvik", result);
    }

    [Fact]
    public void Normalize_KeepsRelativeSegments()
    {
        // Регрессия аудита: MediaTranscodePathPolicy использовал Path.GetFileName,
        // из-за чего подкаталоги терялись и имена из разных папок склеивались.
        var result = MediaPath.Normalize("Alerts/random_meme/videos/file_103.mp4");

        Assert.Equal("Alerts/random_meme/videos/file_103.mp4", result);
        Assert.Equal("file_103.mp4", MediaPath.GetFileName(result));
    }

    [Fact]
    public void Normalize_EmptyOrNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MediaPath.Normalize(string.Empty));
        Assert.Equal(string.Empty, MediaPath.Normalize("   "));
        Assert.Equal(string.Empty, MediaPath.Normalize(null));
    }

    [Fact]
    public void Normalize_PreservesDoubleDotSegments()
    {
        // Нормализация не должна «съедать" выход за пределы хранилища:
        // безопасность проверяет отдельный валидатор, здесь только разделители.
        var result = MediaPath.Normalize("Alerts/../etc/passwd");

        Assert.Equal("Alerts/../etc/passwd", result);
    }

    [Theory]
    [InlineData("Alerts/zvik/cat.mp3", true)]
    [InlineData("Alerts\\zvik\\cat.mp3", true)]
    [InlineData("../etc/passwd", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("Alerts/../../etc/passwd", false)]
    [InlineData("C:/Windows/system32", false)]
    [InlineData("", false)]
    public void IsSafeRelative_RejectsTraversal(string path, bool expected)
    {
        var result = MediaPath.IsSafeRelative(path);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSafeRelative_RejectsBackslashTraversal()
    {
        // Обратный слеш тоже должен проверяться, иначе Windows-путь обходит защиту.
        var result = MediaPath.IsSafeRelative("Alerts\\..\\..\\etc\\passwd");

        Assert.False(result);
    }

    [Fact]
    public void IsSafeRelative_AcceptsStoredUrlFormAfterNormalize()
    {
        // Контракт хранилища: FilePath в БД/URL хранится с ведущим «/», поэтому
        // проверять безопасность нужно ПОСЛЕ Normalize. Без этого легитимный
        // путь отсекался бы как абсолютный, а "../.." проходил бы.
        var stored = "/Alerts/zvik/cat.mp3";

        Assert.False(MediaPath.IsSafeRelative(stored));
        Assert.True(MediaPath.IsSafeRelative(MediaPath.Normalize(stored)));
        Assert.False(MediaPath.IsSafeRelative(MediaPath.Normalize("/../etc/passwd")));
    }

    [Fact]
    public void Combine_JoinsSegments()
    {
        var result = MediaPath.Combine("Alerts/zvik", "cat.mp3");

        Assert.Equal("Alerts/zvik/cat.mp3", result);
    }

    [Fact]
    public void Combine_ResolvesRelativeAgainstBase()
    {
        var result = MediaPath.Combine("Alerts/random_meme/videos", "file_103.mp4");

        Assert.Equal("Alerts/random_meme/videos/file_103.mp4", result);
    }

    [Theory]
    [InlineData("Alerts/random_meme/file.mp4", true)]
    [InlineData("Alerts/.private/file.mp4", false)]
    [InlineData("Alerts/.git/config", false)]
    [InlineData("Alerts/_trash/deleted.mp4", false)]
    [InlineData(".hidden/file.mp4", false)]
    [InlineData("Alerts/visible/file.mp4", true)]
    public void IsVisibleRelativePath_AppliesOnePolicyToEverySegment(string path, bool expected)
    {
        Assert.Equal(expected, MediaPath.IsVisibleRelativePath(path));
    }

    [Theory]
    [InlineData("visible", true)]
    [InlineData(".hidden", false)]
    [InlineData(".git", false)]
    [InlineData("_trash", false)]
    [InlineData("..", false)]
    public void IsVisibleSegment_RejectsInternalSegments(string segment, bool expected)
    {
        Assert.Equal(expected, MediaPath.IsVisibleSegment(segment));
    }

    [Fact]
    public void GetContentType_ForKnownExtension()
    {
        Assert.Equal("video/mp4", MediaPath.GetContentType("a.mp4"));
        Assert.Equal("audio/mpeg", MediaPath.GetContentType("a.mp3"));
        Assert.Equal("image/jpeg", MediaPath.GetContentType("a.jpg"));
        Assert.Equal("image/svg+xml", MediaPath.GetContentType("a.svg"));
        Assert.Equal("video/webm", MediaPath.GetContentType("a.webm"));
    }

    [Fact]
    public void GetContentType_ForUnknownExtension_FallsBackToOctetStream()
    {
        Assert.Equal("application/octet-stream", MediaPath.GetContentType("a.bin"));
    }

    [Fact]
    public void GetContentType_IsCaseInsensitive()
    {
        Assert.Equal("video/mp4", MediaPath.GetContentType("a.MP4"));
    }
}
