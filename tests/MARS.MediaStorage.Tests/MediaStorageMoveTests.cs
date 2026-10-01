using MARS.MediaStorage.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: массовое перемещение обязано обновлять путь в БД, иначе
/// записи указывают на несуществующие файлы, а перенос в корзину ломается.
/// </summary>
public class MediaStorageMoveTests
{
    private static async Task<Dictionary<string, Guid>> SeedAsync(
        StorageTestContext ctx,
        params string[] paths
    )
    {
        foreach (var p in paths)
        {
            ctx.WriteFile(p);
        }

        await ctx.CreateService().IndexAsync(CancellationToken.None);

        await using var db = ctx.Factory.CreateDbContext();
        return await db.MediaEntries.ToDictionaryAsync(
            e => e.Path,
            e => e.Id,
            TestContext.Current.CancellationToken
        );
    }

    [Fact]
    public async Task MoveAsync_MovesFilesAndUpdatesPaths()
    {
        using var ctx = new StorageTestContext();
        var ids = await SeedAsync(ctx, "Alerts/videos/a.mp4", "Alerts/videos/b.mp4");
        var service = ctx.CreateService();

        var result = await service.MoveAsync(
            [ids["Alerts/videos/a.mp4"], ids["Alerts/videos/b.mp4"]],
            "Archive/2026",
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(2, result.Succeeded);
        Assert.True(ctx.Exists("Archive/2026/a.mp4"));
        Assert.True(ctx.Exists("Archive/2026/b.mp4"));
        Assert.False(ctx.Exists("Alerts/videos/a.mp4"));

        await using var db = ctx.Factory.CreateDbContext();
        var paths = await db
            .MediaEntries.Select(e => e.Path)
            .OrderBy(p => p)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Archive/2026/a.mp4", "Archive/2026/b.mp4"], paths);
    }

    [Fact]
    public async Task MoveAsync_DryRun_ChangesNothing()
    {
        using var ctx = new StorageTestContext();
        var ids = await SeedAsync(ctx, "Alerts/videos/a.mp4");
        var service = ctx.CreateService();

        var result = await service.MoveAsync(
            [ids["Alerts/videos/a.mp4"]],
            "Archive/2026",
            dryRun: true,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, result.Succeeded);
        Assert.True(ctx.Exists("Alerts/videos/a.mp4"));
        Assert.False(ctx.Exists("Archive/2026/a.mp4"));
        await using var db = ctx.Factory.CreateDbContext();
        Assert.Equal(
            "Alerts/videos/a.mp4",
            (await db.MediaEntries.SingleAsync(TestContext.Current.CancellationToken)).Path
        );
    }

    [Fact]
    public async Task MoveAsync_RejectsOccupiedTarget()
    {
        // Перезапись молча уничтожила бы чужой файл.
        using var ctx = new StorageTestContext();
        var ids = await SeedAsync(ctx, "Alerts/videos/a.mp4", "Archive/a.mp4");
        var service = ctx.CreateService();

        var result = await service.MoveAsync(
            [ids["Alerts/videos/a.mp4"]],
            "Archive",
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.True(ctx.Exists("Alerts/videos/a.mp4"));
        Assert.Equal(
            "x",
            await File.ReadAllTextAsync(
                Path.Combine(ctx.Root, "Archive", "a.mp4"),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task MoveAsync_RejectsTraversalTarget()
    {
        using var ctx = new StorageTestContext();
        var ids = await SeedAsync(ctx, "Alerts/a.mp4");
        var service = ctx.CreateService();

        var result = await service.MoveAsync(
            [ids["Alerts/a.mp4"]],
            "../../outside",
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, result.Succeeded);
        Assert.True(ctx.Exists("Alerts/a.mp4"));
    }

    [Fact]
    public async Task MoveAsync_RefusesToMoveIntoTrash()
    {
        // Иначе «перенос» стал бы обходным путём для безвозвратного удаления
        // в обход окна восстановления.
        using var ctx = new StorageTestContext();
        var ids = await SeedAsync(ctx, "Alerts/a.mp4");
        var service = ctx.CreateService();

        var result = await service.MoveAsync(
            [ids["Alerts/a.mp4"]],
            "_trash/manual",
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, result.Succeeded);
        Assert.True(ctx.Exists("Alerts/a.mp4"));
    }

    [Fact]
    public async Task MoveAsync_PartialFailureKeepsSucceededFiles()
    {
        // Ошибка на одном файле не должна откатывать уже перенесённые.
        using var ctx = new StorageTestContext();
        var ids = await SeedAsync(ctx, "Alerts/videos/a.mp4", "Alerts/videos/b.mp4");
        ctx.WriteFile("Archive/b.mp4");
        var service = ctx.CreateService();

        var result = await service.MoveAsync(
            [ids["Alerts/videos/a.mp4"], ids["Alerts/videos/b.mp4"]],
            "Archive",
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.True(ctx.Exists("Archive/a.mp4"));
        Assert.True(ctx.Exists("Alerts/videos/b.mp4"));
    }

    [Fact]
    public async Task MoveAsync_CommitsOnceForBatch()
    {
        // На каждый файл отдельный коммит превратил бы перенос 500 файлов в
        // 500 коммитов; достаточно одного на операцию.
        using var ctx = new StorageTestContext();
        var ids = await SeedAsync(ctx, "Alerts/a.mp4", "Alerts/b.mp4");
        var service = ctx.CreateService();

        await service.MoveAsync(
            [ids["Alerts/a.mp4"], ids["Alerts/b.mp4"]],
            "Archive",
            dryRun: false,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(1, ctx.Git.SyncCalls);
    }
}
