using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

public sealed class MediaStorageSearchTests
{
    [Fact]
    public async Task SearchAsync_ReturnsCaseInsensitivePagedResults()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/Clip-a.mp4", "a");
        context.WriteFile("Alerts/clip-b.mp4", "bb");
        context.WriteFile("TTS/other.mp3", "ccc");

        var storage = context.CreateService();
        await storage.IndexAsync(TestContext.Current.CancellationToken);

        var firstPage = await storage.SearchAsync(
            "CLIP",
            page: 1,
            pageSize: 1,
            cancellationToken: TestContext.Current.CancellationToken
        );
        var secondPage = await storage.SearchAsync(
            "clip",
            page: 2,
            pageSize: 1,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(2, firstPage.Total);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(1, firstPage.PageSize);
        Assert.Equal(["Alerts/Clip-a.mp4"], firstPage.Entries.Select(entry => entry.Path));

        Assert.Equal(2, secondPage.Total);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(["Alerts/clip-b.mp4"], secondPage.Entries.Select(entry => entry.Path));
    }

    [Fact]
    public async Task SearchAsync_ExcludesSoftDeletedEntriesAndClampsPagination()
    {
        using var context = new StorageTestContext();
        context.WriteFile("Alerts/keep.mp4");
        context.WriteFile("Alerts/deleted.mp4");

        var storage = context.CreateService();
        await storage.IndexAsync(TestContext.Current.CancellationToken);

        await using var db = context.Factory.CreateDbContext();
        var deletedId = await db.MediaEntries
            .Where(entry => entry.Path == "Alerts/deleted.mp4")
            .Select(entry => entry.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        await storage.SoftDeleteAsync(
            [deletedId],
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await storage.SearchAsync(
            string.Empty,
            page: 0,
            pageSize: 1000,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(["Alerts/keep.mp4"], result.Entries.Select(entry => entry.Path));
    }
}
