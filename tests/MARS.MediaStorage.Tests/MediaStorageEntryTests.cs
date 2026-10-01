using MARS.MediaStorage.Entities;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: у метаданных хранилища не было состояния удаления, поэтому
/// «мягкое удаление» и «окно восстановления» невозможно было выразить и
/// протестировать. Здесь фиксируется контракт состояний записи.
/// </summary>
public class MediaStorageEntryTests
{
    private static MediaStorageEntry CreateEntry() =>
        new()
        {
            Path = "Alerts/random_meme/videos/a.mp4",
            FileName = "a.mp4",
            Extension = ".mp4",
            MediaType = MediaType.Video,
            SizeBytes = 1024,
            UploadedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

    [Fact]
    public void NewEntry_IsNotDeleted()
    {
        var entry = CreateEntry();

        Assert.False(entry.IsDeleted);
        Assert.Null(entry.DeletedAt);
        Assert.Null(entry.OriginalPath);
        Assert.Null(entry.LastDownloadedAt);
    }

    [Fact]
    public void IsDeleted_AfterSoftDelete()
    {
        var entry = CreateEntry();
        var now = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);

        entry.DeletedAt = now;
        entry.OriginalPath = entry.Path;
        entry.Path = "_trash/Ab/cd/abcd-a.mp4";

        Assert.True(entry.IsDeleted);
        Assert.Equal(now, entry.DeletedAt);
        Assert.Equal("Alerts/random_meme/videos/a.mp4", entry.OriginalPath);
    }

    [Fact]
    public void IsPurgeable_OnlyAfterRetentionElapsed()
    {
        var entry = CreateEntry();
        var deletedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        entry.DeletedAt = deletedAt;

        var retention = TimeSpan.FromDays(30);

        Assert.False(entry.IsPurgeable(deletedAt.AddDays(29), retention));
        Assert.True(entry.IsPurgeable(deletedAt.AddDays(30), retention));
        Assert.True(entry.IsPurgeable(deletedAt.AddDays(31), retention));
    }

    [Fact]
    public void IsPurgeable_FalseForLiveFile()
    {
        // Живой файл нельзя удалять безвозвратно даже спустя год.
        var entry = CreateEntry();

        Assert.False(entry.IsPurgeable(DateTimeOffset.MaxValue, TimeSpan.FromDays(30)));
    }

    [Fact]
    public void IsPurgeable_IgnoresNegativeElapsedTime()
    {
        // Часы могут идти назад (NTP, правка вручную) — тогда удалять нельзя.
        var entry = CreateEntry();
        var deletedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        entry.DeletedAt = deletedAt;

        Assert.False(
            entry.IsPurgeable(deletedAt.AddDays(-5), TimeSpan.FromDays(30))
        );
    }
}
