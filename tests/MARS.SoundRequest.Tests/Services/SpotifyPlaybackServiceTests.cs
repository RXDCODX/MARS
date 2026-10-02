using System.Net;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Data;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.Spotify;
using MARS.SoundRequest.Tests.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Обёртка плеера Spotify: она решает, Spotify это или нет, и достаёт id трека
/// из <c>VideoId</c> либо из ссылки. Отказ здесь — не «сбой Spotify», а «это не
/// трек Spotify», и он обязан отличаться от первого.
/// </summary>
public class SpotifyPlaybackServiceTests
{
    private readonly SpotifyPlaybackService _service = Create(true);

    public SpotifyPlaybackServiceTests() => SpotifyStubHandler.Shared.Reset();

    [Theory]
    [InlineData("spotify:track:abc", true)]
    [InlineData("SPOTIFY:TRACK:abc", true)]
    [InlineData("youtube:abc", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void SpotifyTrackIsRecognizedByVideoId(string videoId, bool expected)
    {
        Assert.Equal(expected, _service.IsSpotifyTrack(Track(videoId)));
    }

    [Fact]
    public void NullTrackIsNotSpotifyTrack()
    {
        Assert.False(_service.IsSpotifyTrack(null));
    }

    [Fact]
    public void TrackIdIsTakenFromVideoId()
    {
        Assert.Equal("abc", _service.GetSpotifyTrackId(Track("spotify:track:abc")));
    }

    [Fact]
    public void TrackIdIsTakenFromSpotifyUrl()
    {
        var track = Track("abc", "https://open.spotify.com/track/abc");

        Assert.Equal("abc", _service.GetSpotifyTrackId(track));
    }

    [Fact]
    public void ForeignUrlYieldsNoTrackId()
    {
        var track = Track("abc", "https://example.org/abc");

        Assert.Null(_service.GetSpotifyTrackId(track));
    }

    [Fact]
    public void NullTrackYieldsNoTrackId()
    {
        Assert.Null(_service.GetSpotifyTrackId(null));
    }

    [Fact]
    public void TrackWithoutVideoIdAndUrlYieldsNoTrackId()
    {
        Assert.Null(_service.GetSpotifyTrackId(Track("  ")));
    }

    /// <summary>
    /// Трек не из Spotify плеером не трогается: без проверки он отправил бы
    /// запрос воспроизведения с чужим id и получил бы ошибку Spotify вместо
    /// тихого «это не наш провайдер».
    /// </summary>
    [Fact]
    public async Task NonSpotifyTrackIsNotPlayed()
    {
        Assert.False(
            await _service.PlayTrackAsync(
                Track("youtube:abc"),
                TestContext.Current.CancellationToken
            )
        );
        Assert.Empty(SpotifyStubHandler.Shared.Requests);
    }

    [Fact]
    public async Task NullTrackIsNotPlayed()
    {
        Assert.False(await _service.PlayTrackAsync(null, TestContext.Current.CancellationToken));
        Assert.Empty(SpotifyStubHandler.Shared.Requests);
    }

    [Fact]
    public async Task SpotifyTrackIsPlayedByUri()
    {
        SpotifyStubHandler.Shared.Respond("{}");

        Assert.True(
            await _service.PlayTrackAsync(
                Track("spotify:track:abc"),
                TestContext.Current.CancellationToken
            )
        );
        Assert.Contains("spotify:track:abc", SpotifyStubHandler.Shared.LastBody);
    }

    [Fact]
    public async Task RejectedPlayIsReported()
    {
        SpotifyStubHandler.Shared.Respond("{}", HttpStatusCode.NotFound);

        Assert.False(
            await _service.PlayTrackAsync(
                Track("spotify:track:abc"),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task PauseResumeAndSkipReachTheirEndpoints()
    {
        SpotifyStubHandler.Shared.Respond("{}");

        Assert.True(await _service.PauseAsync(TestContext.Current.CancellationToken));
        Assert.Contains("/pause", SpotifyStubHandler.Shared.LastUrl);

        Assert.True(await _service.ResumeAsync(TestContext.Current.CancellationToken));
        Assert.Contains("/play", SpotifyStubHandler.Shared.LastUrl);

        Assert.True(await _service.SkipAsync(TestContext.Current.CancellationToken));
        Assert.Contains("/next", SpotifyStubHandler.Shared.LastUrl);
    }

    /// <summary>
    /// «Стоп» для Spotify — это пауза: своего состояния остановки у Spotify нет,
    /// а снятие паузы сбросило бы воспроизведение у слушателя.
    /// </summary>
    [Fact]
    public async Task StopPausesInsteadOfResuming()
    {
        SpotifyStubHandler.Shared.Respond("{}");

        Assert.True(await _service.StopAsync(TestContext.Current.CancellationToken));
        Assert.Contains("/pause", SpotifyStubHandler.Shared.LastUrl);
    }

    [Fact]
    public async Task VolumeIsPassedThrough()
    {
        SpotifyStubHandler.Shared.Respond("{}");

        Assert.True(await _service.SetVolumeAsync(40, TestContext.Current.CancellationToken));
        Assert.Contains("volume_percent=40", SpotifyStubHandler.Shared.LastUrl);
    }

    [Fact]
    public async Task PlaybackStateIsPassedThrough()
    {
        SpotifyStubHandler.Shared.Respond(
            """{"is_playing":true,"progress_ms":10,"item":{"id":"abc","duration_ms":20}}"""
        );

        var snapshot = await _service.GetCurrentPlaybackAsync(
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(snapshot);
        Assert.Equal("abc", snapshot!.TrackId);
        Assert.True(snapshot.IsPlaying);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfiguredFlagIsTakenFromApiClient(bool enabled)
    {
        Assert.Equal(enabled, Create(enabled).IsConfigured());
    }

    private static BaseTrackInfo Track(string videoId, string? url = null) =>
        new()
        {
            VideoId = videoId,
            TrackName = "трек",
            Url = url is null ? new Uri("https://example.org/track") : new Uri(url),
        };

    private static SpotifyPlaybackService Create(bool enabled)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"spotify-playback-{Guid.NewGuid():N}")
            .Options;
        var factory = new TestDbContextFactory(options);
        using (var database = factory.CreateDbContext())
        {
            database.Database.EnsureCreated();
            Seed(database);
        }

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(SpotifyStubHandler.Shared, disposeHandler: false));
        var auth = new SpotifyAuthService(
            factory,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );
        var apiClient = new SpotifyApiClient(
            new HttpClient(SpotifyStubHandler.Shared, disposeHandler: false),
            auth,
            Options.Create(new SpotifySoundRequestConfiguration { Enabled = enabled }),
            NullLogger<SpotifyApiClient>.Instance
        );

        return new SpotifyPlaybackService(apiClient);
    }

    private static void Seed(MediaDbContext database)
    {
        database.RootState.Add(
            new RootState { Name = RootStateKeys.SoundRequestSpotifyClientId, Value = "client" }
        );
        database.RootState.Add(
            new RootState { Name = RootStateKeys.SoundRequestSpotifyClientSecret, Value = "secret" }
        );
        database.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.SoundRequestSpotifyRefreshToken,
                Value = "refresh",
            }
        );
        database.RootState.Add(
            new RootState { Name = RootStateKeys.SoundRequestSpotifyAccessToken, Value = "token" }
        );
        database.RootState.Add(
            new RootState
            {
                Name = RootStateKeys.SoundRequestSpotifyAccessTokenExpiresAtUtc,
                Value = DateTime.UtcNow.AddHours(1).ToString("O"),
            }
        );
        database.SaveChanges();
    }
}
