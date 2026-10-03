using MARS.Telegram.Data;
using MARS.Telegram.Services.Booru;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Telegram.Tests;

/// <summary>
/// Дедупликация публикаций по тройке «источник + id изображения + канал».
/// </summary>
public class DeduplicationServiceTests : IDisposable
{
    private readonly ChatTestDbContextFactory _factory = new();

    private (DeduplicationService Service, IDbContextFactory<ChatDbContext> Factory) Build() =>
        (new DeduplicationService(_factory, NullLogger<DeduplicationService>.Instance), _factory);

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task IsAlreadyPostedAsync_ReturnsFalse_ForUnseenImage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build();

        var result = await service.IsAlreadyPostedAsync("rule34", 7, 1234567890, ct);

        Assert.True(result.Success);
        Assert.False(result.Result);
    }

    [Fact]
    public async Task RecordPostAsync_MakesTheImageVisibleToTheCheck()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build();

        var written = await service.RecordPostAsync("rule34", 7, 1234567890, ct);
        var check = await service.IsAlreadyPostedAsync("rule34", 7, 1234567890, ct);

        Assert.True(written.Success);
        Assert.True(check.Result);
    }

    /// <summary>
    /// Ключ дедупликации — тройка: то же изображение в другом канале или из
    /// другого источника публикуется заново, иначе правила для разных каналов
    /// конфликтовали бы за одну отметку.
    /// </summary>
    [Theory]
    [InlineData("danbooru", 7, 1234567890UL)]
    [InlineData("rule34", 8, 1234567890UL)]
    [InlineData("rule34", 7, 9876543210UL)]
    public async Task IsAlreadyPostedAsync_TreatsOtherTriplesAsUnseen(
        string source,
        int imageId,
        ulong channelId
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build();

        await service.RecordPostAsync("rule34", 7, 1234567890, ct);
        var result = await service.IsAlreadyPostedAsync(source, imageId, channelId, ct);

        Assert.False(result.Result);
    }
}
