using MARS.Videos365.Data;
using MARS.Videos365.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Videos365.Tests;

/// <summary>
/// Проверки схемы videos365.Videos365.
/// Утверждения берутся из собранной модели EF: HasDefaultSchema, HasIndex и
/// IsNullable — реляционные аннотации, и они присутствуют в модели независимо
/// от провайдера.
/// </summary>
public class Videos365ModelTests
{
    private static Videos365DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Videos365DbContext>()
            .UseInMemoryDatabase("test")
            .Options;

        return new Videos365DbContext(options);
    }

    [Fact]
    public void Video365_UsesVideos365Schema()
    {
        using var context = CreateContext();

        var schema = context.Model.FindEntityType(typeof(Video365))!.GetSchema();

        Assert.Equal("videos365", schema);
    }

    /// <summary>
    /// SiteId — ключ дедупликации. В монолите индекс на него отсутствовал, и каждая
    /// проверка «выложено ли уже» читала таблицу целиком.
    /// </summary>
    [Fact]
    public void Video365_SiteIdIsUnique()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Video365))!;
        var index = entity
            .GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(Video365.SiteId));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Video365_TableNameMatchesLegacy()
    {
        using var context = CreateContext();

        var tableName = context.Model.FindEntityType(typeof(Video365))!.GetTableName();

        Assert.Equal("Videos365", tableName);
    }

    /// <summary>
    /// В монолите DateUpload не заполнялся и лежал как -infinity (DateTime.MinValue
    /// в Npgsql). Переносить его в NOT NULL значило бы зафиксировать выдуманную
    /// дату; nullable позволяет хранить «дата неизвестна» честно.
    /// </summary>
    [Fact]
    public void Video365_DateUploadIsNullable()
    {
        using var context = CreateContext();

        var property = context
            .Model.FindEntityType(typeof(Video365))!
            .FindProperty(nameof(Video365.DateUpload));

        Assert.True(property!.IsNullable);
    }

    [Fact]
    public async Task Video365_PersistsUploadedMarker()
    {
        await using var context = new Videos365DbContext(
            new DbContextOptionsBuilder<Videos365DbContext>()
                .UseInMemoryDatabase(nameof(Video365_PersistsUploadedMarker))
                .Options
        );

        context.Videos365.Add(
            new Video365
            {
                SiteId = 4242,
                Title = "title",
                PlayerUrl = "player",
                DirectLinkUrl = "direct",
                Description = "description",
                DownloadUrl = "download",
                IsUploaded = true,
                Duration = TimeSpan.FromMinutes(3),
                VideoWidth = 1920,
                VideoHeight = 1080,
                TelegramMessageId = 0,
                DateUpload = null,
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verify = new Videos365DbContext(
            new DbContextOptionsBuilder<Videos365DbContext>()
                .UseInMemoryDatabase(nameof(Video365_PersistsUploadedMarker))
                .Options
        );
        var stored = await verify.Videos365.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(4242, stored.SiteId);
        Assert.True(stored.IsUploaded);
        Assert.Equal(TimeSpan.FromMinutes(3), stored.Duration);
        Assert.Null(stored.DateUpload);
    }
}
