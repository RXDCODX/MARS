using MARS.MediaStorage.Services.Storage;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: в UI не было загрузки файлов — хранилище нельзя было
/// наполнить, только просматривать и раскладывать по уже существующим.
/// Загрузка — основная операция хранилища, поэтому фиксируем контракт:
/// дата загрузки, хеш, отказ от перезаписи и фиксация в git.
/// </summary>
public class MediaStorageUploadTests
{
    private static MediaUploadFile MakeFile(
        string name,
        string content,
        string contentType = "video/mp4"
    ) => new(name, new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)), contentType, System.Text.Encoding.UTF8.GetByteCount(content));

    [Fact]
    public async Task UploadAsync_SavesFileAndRegistersEntry()
    {
        using var ctx = new StorageTestContext();
        var result = await ctx.CreateService().UploadAsync(
            [MakeFile("clip.mp4", "video-bytes")],
            "Alerts/random_meme/videos"
        );

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.True(ctx.Exists("Alerts/random_meme/videos/clip.mp4"));
        Assert.Equal("video-bytes", await File.ReadAllTextAsync(
            Path.Combine(ctx.Root, "Alerts", "random_meme", "videos", "clip.mp4")));
    }

    [Fact]
    public async Task UploadAsync_StampsUploadTimeNotFileTime()
    {
        using var ctx = new StorageTestContext();
        await ctx.CreateService().UploadAsync([MakeFile("a.mp4", "x")], "Uploads");

        // Дата загрузки — момент приёма файла, а не время изменения файла на
        // диске: иначе у всех загрузок была бы дата индексации.
        var entries = await ctx.CreateService().ListAsync();
        var entry = Assert.Single(entries);
        Assert.Equal(ctx.Now, entry.UploadedAt);
    }

    [Fact]
    public async Task UploadAsync_RecordsSizeTypeAndHash()
    {
        using var ctx = new StorageTestContext();
        await ctx.CreateService().UploadAsync(
            [MakeFile("a.mp3", "abc", "audio/mpeg")],
            "Uploads"
        );

        var entry = Assert.Single(await ctx.CreateService().ListAsync());
        Assert.Equal(3, entry.SizeBytes);
        Assert.Equal(MARS.Shared.Models.Media.MediaType.Audio, entry.MediaType);
        Assert.NotNull(entry.ContentHash);
        Assert.Equal(64, entry.ContentHash!.Length);
        Assert.Null(entry.LastDownloadedAt);
    }

    [Fact]
    public async Task UploadAsync_CommitsToGit()
    {
        using var ctx = new StorageTestContext();
        await ctx.CreateService().UploadAsync([MakeFile("a.mp4", "x")], "Uploads");

        Assert.Equal(1, ctx.Git.SyncCalls);
    }

    [Fact]
    public async Task UploadAsync_CommitsOnceForBatch()
    {
        using var ctx = new StorageTestContext();
        await ctx.CreateService().UploadAsync(
            [MakeFile("a.mp4", "x"), MakeFile("b.mp4", "y"), MakeFile("c.mp4", "z")],
            "Uploads"
        );

        Assert.Equal(1, ctx.Git.SyncCalls);
        Assert.Equal(3, (await ctx.CreateService().ListAsync()).Count);
    }

    [Fact]
    public async Task UploadAsync_RefusesToOverwriteExistingFile()
    {
        // Молчаливую перезапись содержимого нельзя допускать: файл в хранилище
        // уже отдан клиентам, и безвозвратная подмена неожиданна.
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Uploads/a.mp4", "старое содержимое");

        var result = await ctx.CreateService().UploadAsync(
            [MakeFile("a.mp4", "новое")],
            "Uploads"
        );

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(
            "старое содержимое",
            await File.ReadAllTextAsync(Path.Combine(ctx.Root, "Uploads", "a.mp4"))
        );
    }

    [Fact]
    public async Task UploadAsync_RejectsTraversalInTargetDirectory()
    {
        using var ctx = new StorageTestContext();
        var result = await ctx.CreateService().UploadAsync(
            [MakeFile("evil.mp4", "x")],
            "../../outside"
        );

        Assert.Equal(0, result.Succeeded);
        Assert.False(File.Exists(Path.Combine(ctx.Root, "..", "..", "outside", "evil.mp4")));
    }

    [Fact]
    public async Task UploadAsync_RejectsTraversalInFileName()
    {
        // Имя файла приходит от клиента и тоже может содержать «..».
        using var ctx = new StorageTestContext();
        var result = await ctx.CreateService().UploadAsync(
            [MakeFile("../escape.mp4", "x")],
            "Uploads"
        );

        Assert.Equal(0, result.Succeeded);
    }

    [Fact]
    public async Task UploadAsync_RejectsEmptyFile()
    {
        using var ctx = new StorageTestContext();
        var result = await ctx.CreateService().UploadAsync(
            [MakeFile("empty.mp4", "")],
            "Uploads"
        );

        Assert.Equal(0, result.Succeeded);
        Assert.False(ctx.Exists("Uploads/empty.mp4"));
    }

    [Fact]
    public async Task UploadAsync_PartialBatchKeepsSucceededFiles()
    {
        // Ошибка на одном файле не должна откатывать уже загруженные.
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Uploads/b.mp4", "занято");

        var result = await ctx.CreateService().UploadAsync(
            [MakeFile("a.mp4", "x"), MakeFile("b.mp4", "y")],
            "Uploads"
        );

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.True(ctx.Exists("Uploads/a.mp4"));
        Assert.Equal("занято", await File.ReadAllTextAsync(
            Path.Combine(ctx.Root, "Uploads", "b.mp4")));
    }

    [Fact]
    public async Task UploadAsync_NormalizesTargetDirectory()
    {
        using var ctx = new StorageTestContext();
        await ctx.CreateService().UploadAsync(
            [MakeFile("a.mp4", "x")],
            "/Alerts/random_meme/videos/"
        );

        Assert.True(ctx.Exists("Alerts/random_meme/videos/a.mp4"));
    }

    [Fact]
    public async Task UploadAsync_RejectsFileAboveConfiguredLimit()
    {
        // Файл крупнее лимита GitHub закоммитить нельзя, поэтому принимать его
        // в хранилище бессмысленно: он остался бы на диске, а push падал бы.
        using var ctx = new StorageTestContext();
        ctx.MaxUploadBytes = 8;

        var result = await ctx.CreateService().UploadAsync(
            [MakeFile("big.mp4", new string('x', 64))],
            "Uploads"
        );

        Assert.Equal(0, result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("превышает лимит"));
        Assert.False(ctx.Exists("Uploads/big.mp4"));
    }

    [Fact]
    public async Task UploadAsync_SurvivesRescanWithoutLosingUploadTime()
    {
        // Повторная индексация не должна превращать загрузку в «файл из папки».
        using var ctx = new StorageTestContext();
        var service = ctx.CreateService();
        await service.UploadAsync([MakeFile("a.mp4", "x")], "Uploads");

        var uploadedAt = Assert.Single(await service.ListAsync()).UploadedAt;
        ctx.Time.Advance(TimeSpan.FromDays(3));
        await service.IndexAsync(CancellationToken.None);

        Assert.Equal(uploadedAt, Assert.Single(await service.ListAsync()).UploadedAt);
    }
}
