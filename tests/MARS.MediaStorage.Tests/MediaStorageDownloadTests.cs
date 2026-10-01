using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: дата последней выгрузки не фиксировалась нигде, поэтому по
/// хранилищу было невозможно понять, какие файлы вообще читают.
/// </summary>
public class MediaStorageDownloadTests
{
    [Fact]
    public async Task MarkDownloadedAsync_SetsTimestamp()
    {
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4");
        await ctx.CreateService().IndexAsync(CancellationToken.None);

        var service = ctx.CreateService();
        ctx.Time.Advance(TimeSpan.FromHours(5));

        await service.MarkDownloadedAsync("Alerts/a.mp4", CancellationToken.None);

        await using var db = ctx.Factory.CreateDbContext();
        var entry = await db.MediaEntries.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ctx.Time.GetUtcNow(), entry.LastDownloadedAt);
    }

    [Fact]
    public async Task MarkDownloadedAsync_AdvancesTimestampOnNextRead()
    {
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4");
        await ctx.CreateService().IndexAsync(CancellationToken.None);
        var service = ctx.CreateService();

        await service.MarkDownloadedAsync("Alerts/a.mp4", CancellationToken.None);

        DateTimeOffset first;
        await using (var db = ctx.Factory.CreateDbContext())
        {
            first = (await db.MediaEntries.SingleAsync(TestContext.Current.CancellationToken))
                .LastDownloadedAt!
                .Value;
        }

        ctx.Time.Advance(TimeSpan.FromDays(3));
        await service.MarkDownloadedAsync("Alerts/a.mp4", CancellationToken.None);

        await using var check = ctx.Factory.CreateDbContext();
        var second = (await check.MediaEntries.SingleAsync(TestContext.Current.CancellationToken))
            .LastDownloadedAt!
            .Value;
        Assert.True(second > first);
    }

    [Fact]
    public async Task MarkDownloadedAsync_IgnoresUnknownPath()
    {
        using var ctx = new StorageTestContext();

        var service = ctx.CreateService();

        // Файл мог быть удалён между индексацией и выдачей — это не ошибка.
        await service.MarkDownloadedAsync("Alerts/нет-такого.mp4", CancellationToken.None);

        await using var db = ctx.Factory.CreateDbContext();
        Assert.Equal(0, await db.MediaEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MarkDownloadedAsync_DoesNotCommitToGit()
    {
        // Выгрузка не меняет содержимое хранилища: коммитить нечего, иначе
        // каждое чтение видео плодило бы коммит.
        using var ctx = new StorageTestContext();
        ctx.WriteFile("Alerts/a.mp4");
        await ctx.CreateService().IndexAsync(CancellationToken.None);

        await ctx.CreateService().MarkDownloadedAsync("Alerts/a.mp4", CancellationToken.None);

        Assert.Equal(0, ctx.Git.SyncCalls);
    }
}
