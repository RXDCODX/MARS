using MARS.MediaStorage.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: индексация хранилища. Без неё дата загрузки неизвестна,
/// UI нечего показать, а массовые операции не знают, какие записи вести.
/// </summary>
public class MediaStorageIndexTests
{
    [Fact]
    public async Task IndexAsync_RegistersExistingFiles()
    {
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/random_meme/videos/a.mp4", "aaa");
        ctx.WriteFile("flags/ru.svg", "<svg/>");

        var result = await ctx.CreateService().IndexAsync(CancellationToken.None);

        Assert.Equal(2, result.Added);

        await using var db = ctx.Factory.CreateDbContext();
        var entries = await db
            .MediaEntries.OrderBy(e => e.Path)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(
            ["Alerts/random_meme/videos/a.mp4", "flags/ru.svg"],
            entries.Select(e => e.Path)
        );
        Assert.All(entries, e => Assert.True(e.SizeBytes > 0));
    }

    [Fact]
    public async Task IndexAsync_IgnoresTrashFolder()
    {
        // Иначе удалённые файлы снова попали бы в выдачу как живые.
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4");
        ctx.WriteFile("_trash/2026-03-15/deadbeef/a.mp4");

        await ctx.CreateService().IndexAsync(CancellationToken.None);

        await using var db = ctx.Factory.CreateDbContext();
        var paths = await db
            .MediaEntries.Select(e => e.Path)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Alerts/a.mp4"], paths);
    }

    [Fact]
    public async Task IndexAsync_IsIdempotent()
    {
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4");
        var service = ctx.CreateService();

        var first = await service.IndexAsync(CancellationToken.None);
        var second = await service.IndexAsync(CancellationToken.None);

        Assert.Equal(1, first.Added);
        Assert.Equal(0, second.Added);
        Assert.Equal(0, second.Updated);

        await using var db = ctx.Factory.CreateDbContext();
        Assert.Equal(1, await db.MediaEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IndexAsync_PreservesUploadedAtOnRescan()
    {
        // Повторное сканирование не должно обнулять дату загрузки — иначе дата
        // «съезжала» бы в ноль при каждом рестарте. Для уже существующих файлов
        // дата берётся из времени изменения файла, а не из текущего момента,
        // поэтому сверяем неизменность, а не значение часов теста.
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4");
        var service = ctx.CreateService();

        await service.IndexAsync(CancellationToken.None);

        await using (var first = ctx.Factory.CreateDbContext())
        {
            var uploadedAt = await first
                .MediaEntries.Select(e => e.UploadedAt)
                .SingleAsync(TestContext.Current.CancellationToken);

            ctx.Time.Advance(TimeSpan.FromDays(7));
            await service.IndexAsync(CancellationToken.None);

            await using var second = ctx.Factory.CreateDbContext();
            Assert.Equal(
                uploadedAt,
                (
                    await second.MediaEntries.SingleAsync(TestContext.Current.CancellationToken)
                ).UploadedAt
            );
        }
    }

    [Fact]
    public async Task IndexAsync_RefreshesSizeWhenFileChanged()
    {
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4", "small");
        var service = ctx.CreateService();
        await service.IndexAsync(CancellationToken.None);

        const string replacement = "much longer content";
        ctx.WriteFile("Alerts/a.mp4", replacement);
        var result = await service.IndexAsync(CancellationToken.None);

        Assert.Equal(1, result.Updated);
        await using var db = ctx.Factory.CreateDbContext();
        Assert.Equal(
            replacement.Length,
            (await db.MediaEntries.SingleAsync(TestContext.Current.CancellationToken)).SizeBytes
        );
    }

    [Fact]
    public async Task IndexAsync_DoesNotCommitToGit()
    {
        // Индексация — наблюдение, а не изменение хранилища: коммитить
        // нечего, иначе каждый рестарт плодил бы пустые коммиты.
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4");

        await ctx.CreateService().IndexAsync(CancellationToken.None);

        Assert.Equal(0, ctx.Git.SyncCalls);
    }
}
