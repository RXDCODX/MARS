using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: пользователь выбрал мягкое удаление — файл переносится в
/// <c>_trash/</c> внутри wwwroot, остаётся жить на диске и версионируется тем же
/// git, восстанавливается перемещением, а через 30 дней удаляется окончательно.
/// Проверяется, что ничего не теряется и что состояние БД соответствует диску.
/// </summary>
public class MediaStorageSoftDeleteTests
{
    private static async Task SeedAsync(StorageTestContext ctx, params string[] paths)
    {
        foreach (var p in paths)
        {
            ctx.WriteFile(p);
        }

        await ctx.CreateService().IndexAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SoftDeleteAsync_MovesFileToTrashAndMarksEntry()
    {
        using var ctx = new StorageTestContext();
        await SeedAsync(ctx, "Alerts/random_meme/videos/a.mp4");
        var service = ctx.CreateService();

        await using (var db = ctx.Factory.CreateDbContext())
        {
            var id = await db
                .MediaEntries.Select(e => e.Id)
                .SingleAsync(TestContext.Current.CancellationToken);
            var result = await service.SoftDeleteAsync(
                [id],
                dryRun: false,
                TestContext.Current.CancellationToken
            );

            Assert.Equal(1, result.Succeeded);
            Assert.Equal(0, result.Failed);
        }

        Assert.False(ctx.Exists("Alerts/random_meme/videos/a.mp4"));
        var trash = await TrashFileAsync(ctx);
        Assert.NotNull(trash);
        Assert.StartsWith("_trash/", trash!, StringComparison.Ordinal);

        await using var check = ctx.Factory.CreateDbContext();
        var entry = await check.MediaEntries.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(entry.IsDeleted);
        Assert.Equal(ctx.Now, entry.DeletedAt);
        Assert.Equal("Alerts/random_meme/videos/a.mp4", entry.OriginalPath);
    }

    [Fact]
    public async Task SoftDeleteAsync_DryRun_ChangesNothing()
    {
        using var ctx = new StorageTestContext();
        await SeedAsync(ctx, "Alerts/a.mp4");
        var service = ctx.CreateService();

        await using var db = ctx.Factory.CreateDbContext();
        var id = await db
            .MediaEntries.Select(e => e.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        var result = await service.SoftDeleteAsync(
            [id],
            dryRun: true,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, result.Succeeded);
        Assert.True(ctx.Exists("Alerts/a.mp4"));
        await using var check = ctx.Factory.CreateDbContext();
        Assert.False(
            (await check.MediaEntries.SingleAsync(TestContext.Current.CancellationToken)).IsDeleted
        );
    }

    [Fact]
    public async Task SoftDeleteAsync_CommitsToGit()
    {
        using var ctx = new StorageTestContext();
        await SeedAsync(ctx, "Alerts/a.mp4");
        var service = ctx.CreateService();

        await using var db = ctx.Factory.CreateDbContext();
        var id = await db
            .MediaEntries.Select(e => e.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        await service.SoftDeleteAsync([id], dryRun: false, TestContext.Current.CancellationToken);

        Assert.Equal(1, ctx.Git.SyncCalls);
    }

    [Fact]
    public async Task RestoreAsync_MovesFileBackAndClearsDeletion()
    {
        using var ctx = new StorageTestContext();
        await SeedAsync(ctx, "Alerts/random_meme/videos/a.mp4");
        var service = ctx.CreateService();

        Guid id;
        await using (var db = ctx.Factory.CreateDbContext())
        {
            id = await db
                .MediaEntries.Select(e => e.Id)
                .SingleAsync(TestContext.Current.CancellationToken);
            await service.SoftDeleteAsync(
                [id],
                dryRun: false,
                TestContext.Current.CancellationToken
            );
        }

        var restored = await service.RestoreAsync(
            [id],
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, restored);
        Assert.True(ctx.Exists("Alerts/random_meme/videos/a.mp4"));
        await using var check = ctx.Factory.CreateDbContext();
        var entry = await check.MediaEntries.SingleAsync(TestContext.Current.CancellationToken);
        Assert.False(entry.IsDeleted);
        Assert.Null(entry.DeletedAt);
        Assert.Null(entry.OriginalPath);
    }

    [Fact]
    public async Task RestoreAsync_FailsWhenTargetOccupied()
    {
        // Восстановление не имеет права затереть файл, который уже лежит
        // по исходному пути: это привело бы к потере данных.
        using var ctx = new StorageTestContext();
        await SeedAsync(ctx, "Alerts/a.mp4");
        var service = ctx.CreateService();

        Guid id;
        await using (var db = ctx.Factory.CreateDbContext())
        {
            id = await db
                .MediaEntries.Select(e => e.Id)
                .SingleAsync(TestContext.Current.CancellationToken);
            await service.SoftDeleteAsync(
                [id],
                dryRun: false,
                TestContext.Current.CancellationToken
            );
        }

        ctx.WriteFile("Alerts/a.mp4", "новое содержимое");
        var restored = await service.RestoreAsync(
            [id],
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, restored);
        Assert.Equal(
            "новое содержимое",
            await File.ReadAllTextAsync(
                ctx.WriteFile("Alerts/a.mp4", "новое содержимое"),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task PurgeExpiredAsync_DeletesOnlyExpiredFiles()
    {
        using var ctx = new StorageTestContext();
        await SeedAsync(ctx, "Alerts/old.mp4", "Alerts/fresh.mp4");
        var service = ctx.CreateService();

        await using var db = ctx.Factory.CreateDbContext();
        var ids = await db.MediaEntries.ToDictionaryAsync(
            e => e.Path,
            e => e.Id,
            TestContext.Current.CancellationToken
        );
        await service.SoftDeleteAsync(
            [ids["Alerts/old.mp4"], ids["Alerts/fresh.mp4"]],
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        // «Старый» удалён в момент Now и пережил 31 день. «Свежий» надо
        // удалить незадолго до текущего момента, иначе он тоже просрочен.
        ctx.Time.Advance(TimeSpan.FromDays(31));
        var freshNow = ctx.Time.GetUtcNow();
        await using (var adjust = ctx.Factory.CreateDbContext())
        {
            var fresh = await adjust.MediaEntries.SingleAsync(
                e => e.OriginalPath == "Alerts/fresh.mp4",
                TestContext.Current.CancellationToken
            );
            fresh.DeletedAt = freshNow.AddDays(-5);
            await adjust.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var purged = await service.PurgeExpiredAsync(CancellationToken.None);

        Assert.Equal(1, purged);
        await using var check = ctx.Factory.CreateDbContext();
        var left = await check.MediaEntries.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Alerts/fresh.mp4", left.OriginalPath);
    }

    [Fact]
    public async Task PurgeExpiredAsync_KeepsLiveFiles()
    {
        using var ctx = new StorageTestContext();
        await SeedAsync(ctx, "Alerts/live.mp4");
        var service = ctx.CreateService();

        ctx.Time.Advance(TimeSpan.FromDays(365));
        var purged = await service.PurgeExpiredAsync(CancellationToken.None);

        Assert.Equal(0, purged);
        Assert.True(ctx.Exists("Alerts/live.mp4"));
        await using var check = ctx.Factory.CreateDbContext();
        Assert.Equal(1, await check.MediaEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<string?> TrashFileAsync(StorageTestContext ctx)
    {
        var trashRoot = Path.Combine(ctx.Root, "_trash");
        if (!Directory.Exists(trashRoot))
        {
            return null;
        }

        var file = Directory
            .EnumerateFiles(trashRoot, "*", SearchOption.AllDirectories)
            .FirstOrDefault();
        return file is null ? null : Path.GetRelativePath(ctx.Root, file).Replace('\\', '/');
    }
}
