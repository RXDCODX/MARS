using MARS.MediaStorage.Services.Media;

namespace MARS.Microservices.Tests.MediaStorage;

public class MediaTranscodePathPolicyTests
{
    [Fact]
    public void GetTranscodedCachePath_ForSameSource_IsStable()
    {
        var source = Path.Combine("C:", "mars", "Alerts", "clip.webm");
        var root = Path.Combine("C:", "mars", "Alerts", "_converted");

        var first = MediaTranscodePathPolicy.GetTranscodedCachePath(source, root);
        var second = MediaTranscodePathPolicy.GetTranscodedCachePath(source, root);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetTranscodedCachePath_IsNotAdjacentToSource()
    {
        // Регрессия аудита №13: старый код писал foo.mp4 рядом с foo.webm и удалял
        // оригинал. Кэш обязан лежать в отдельной папке и не быть исходником.
        var source = Path.Combine("C:", "mars", "Alerts", "clip.webm");
        var root = Path.Combine("C:", "mars", "Alerts", "_converted");

        var cached = MediaTranscodePathPolicy.GetTranscodedCachePath(source, root);

        Assert.NotEqual(source, cached);
        Assert.Equal(".mp4", Path.GetExtension(cached));
        Assert.Equal(
            Path.GetFullPath(root),
            Path.GetFullPath(Path.GetDirectoryName(cached)!)
        );
    }

    [Fact]
    public void GetTranscodedCachePath_ForDifferentSources_Differs()
    {
        var root = Path.Combine("C:", "mars", "_converted");

        var first = MediaTranscodePathPolicy.GetTranscodedCachePath(
            Path.Combine("C:", "mars", "a.webm"),
            root
        );
        var second = MediaTranscodePathPolicy.GetTranscodedCachePath(
            Path.Combine("C:", "mars", "b.webm"),
            root
        );

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ShouldReuseCache_WhenCacheNewerThanSource_ReturnsTrue()
    {
        var sourceWriteUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var cacheWriteUtc = sourceWriteUtc.AddMinutes(1);

        var result = MediaTranscodePathPolicy.ShouldReuseCache(
            cacheExists: true,
            cacheWriteUtc,
            sourceWriteUtc
        );

        Assert.True(result);
    }

    [Fact]
    public void ShouldReuseCache_WhenCacheOlderThanSource_ReturnsFalse()
    {
        var sourceWriteUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var cacheWriteUtc = sourceWriteUtc.AddMinutes(-1);

        var result = MediaTranscodePathPolicy.ShouldReuseCache(
            cacheExists: true,
            cacheWriteUtc,
            sourceWriteUtc
        );

        Assert.False(result);
    }

    [Fact]
    public void ShouldReuseCache_WhenCacheMissing_ReturnsFalse()
    {
        var result = MediaTranscodePathPolicy.ShouldReuseCache(
            cacheExists: false,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        );

        Assert.False(result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetMirroredWebRootPath_ReturnsPathOnlyInDevelopment(bool isDevelopment)
    {
        var mediaPath = Path.Combine("C:", "mars", "media", "Alerts", "clip.mp4");
        var webRoot = Path.Combine("C:", "mars", "wwwroot");

        var result = MediaTranscodePathPolicy.GetMirroredWebRootPath(
            mediaPath,
            webRoot,
            isDevelopment
        );

        if (isDevelopment)
        {
            Assert.NotNull(result);
            Assert.StartsWith(Path.GetFullPath(webRoot), Path.GetFullPath(result!));
        }
        else
        {
            Assert.Null(result);
        }
    }
}
