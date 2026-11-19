using MARS.Shared.Contracts.Clients;
using MARS.WaifuGacha.Domain.Entities;
using MARS.WaifuGacha.Infrastructure.Postgres.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MARS.WaifuGacha.Tests.Services;

public class MikuMondayServiceTests : IAsyncLifetime
{
    private readonly WaifuTestDbContextFactory _factory = new();
    private readonly MikuMondayService _service;

    public MikuMondayServiceTests()
    {
        _service = new MikuMondayService(_factory, NullLogger<MikuMondayService>.Instance);
    }

    public async ValueTask InitializeAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken
        );
        db.MikuMondayTracks.AddRange(
            new MikuMondayTrack
            {
                Number = 1,
                BaseTrackInfoId = Guid.NewGuid(),
                TrackName = "Track 1",
                Artist = "A",
                Url = "u1",
            },
            new MikuMondayTrack
            {
                Number = 2,
                BaseTrackInfoId = Guid.NewGuid(),
                TrackName = "Track 2",
                Artist = "B",
                Url = "u2",
            },
            new MikuMondayTrack
            {
                Number = 3,
                BaseTrackInfoId = Guid.NewGuid(),
                TrackName = "Track 3",
                Artist = "C",
                Url = "u3",
            }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task UserGetsTrackAndSecondCallFails()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = await _service.GetRandomTrackForUserAsync("u1", "User1", ct);
        Assert.True(first.IsSuccess);
        Assert.NotNull(first.Track);

        var second = await _service.GetRandomTrackForUserAsync("u1", "User1", ct);
        Assert.False(second.IsSuccess);
        Assert.Contains("уже активировали", second.Error);
    }

    [Fact]
    public async Task StreamerDoesNotConsumeTrack()
    {
        var ct = TestContext.Current.CancellationToken;
        var before = await _service.GetAvailableTracksAsync(ct);
        var countBefore = before.Count;

        var streamer = await _service.GetRandomTrackForStreamerAsync(ct);
        Assert.True(streamer.IsSuccess);

        var after = await _service.GetAvailableTracksAsync(ct);
        Assert.Equal(countBefore, after.Count);
    }

    [Fact]
    public async Task AvailableTracksExcludesUsed()
    {
        var ct = TestContext.Current.CancellationToken;
        await _service.GetRandomTrackForUserAsync("u2", "User2", ct);

        var available = await _service.GetAvailableTracksAsync(ct);
        Assert.Equal(2, available.Count);
    }
}
