using System.Net;
using MARS.SoundRequest.Entities;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.YouTube;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Разрешение ссылки YouTube через oEmbed.
///
/// Разрешение идёт без ключа API, поэтому доступны только базовые метаданные.
/// Проверяется разбор ссылки во всех форматах, которые зрители присылают, и то,
/// что название трека читаемо.
/// </summary>
public class YouTubeResolverTests
{
    private const string OEmbedJson = """
        {"title":"Начало","author_name":"Miku","author_url":"https://example.test"}
        """;

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void VideoIdIsExtractedFromEveryLinkForm(string url, string expected)
    {
        Assert.Equal(expected, YouTubeResolver.ExtractVideoId(url));
    }

    /// <summary>
    /// Чужая ссылка не превращается в идентификатор: подставить выдуманный id
    /// значило бы поставить не тот трек.
    /// </summary>
    [Theory]
    [InlineData("https://example.test/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/feed/subscriptions")]
    [InlineData("не ссылка")]
    [InlineData("")]
    [InlineData(null)]
    public void ForeignLinkHasNoVideoId(string? url)
    {
        Assert.Null(YouTubeResolver.ExtractVideoId(url!));
    }

    [Fact]
    public async Task TitleIsPrefixedWithAuthor()
    {
        var handler = new StubHandler(HttpStatusCode.OK, OEmbedJson);
        var resolver = new YouTubeResolver(
            NullLogger<YouTubeResolver>.Instance,
            new SingleHandlerFactory(handler)
        );

        var track = await resolver.ResolveVideoAsync(
            "https://youtu.be/dQw4w9WgXcQ",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Miku - Начало", track!.TrackName);
        Assert.Equal("dQw4w9WgXcQ", track.VideoId);
    }

    /// <summary>
    /// Без автора название остаётся названием: в оверлее не должно быть « - Начало».
    /// </summary>
    [Fact]
    public async Task TitleWithoutAuthorIsUsedAsIs()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"title":"Начало","author_name":""}""");
        var resolver = new YouTubeResolver(
            NullLogger<YouTubeResolver>.Instance,
            new SingleHandlerFactory(handler)
        );

        var track = await resolver.ResolveVideoAsync(
            "https://youtu.be/dQw4w9WgXcQ",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("Начало", track!.TrackName);
    }

    [Fact]
    public async Task UnavailableApiYieldsNoTrack()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, "{}");
        var resolver = new YouTubeResolver(
            NullLogger<YouTubeResolver>.Instance,
            new SingleHandlerFactory(handler)
        );

        Assert.Null(
            await resolver.ResolveVideoAsync(
                "https://youtu.be/dQw4w9WgXcQ",
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>
    /// Чужая ссылка не опрашивается: идентификатор из неё выдумывать нельзя.
    /// </summary>
    [Fact]
    public async Task ForeignLinkIsNotRequested()
    {
        var handler = new StubHandler(HttpStatusCode.OK, OEmbedJson);
        var resolver = new YouTubeResolver(
            NullLogger<YouTubeResolver>.Instance,
            new SingleHandlerFactory(handler)
        );

        await resolver.ResolveVideoAsync(
            "https://example.test/film",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(0, handler.Requests);
    }

    /// <summary>
    /// Поиск по названию без ключа API не поддерживается и честно возвращает пусто:
    /// иначе в очередь попадал бы случайный трек вместо искомого.
    /// </summary>
    [Fact]
    public async Task SearchByQueryIsNotSupported()
    {
        var resolver = new YouTubeResolver(
            NullLogger<YouTubeResolver>.Instance,
            new SingleHandlerFactory(new StubHandler(HttpStatusCode.OK, "{}"))
        );

        Assert.Null(
            await resolver.ResolveQueryAsync("название", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task PlaylistResolutionIsNotSupported()
    {
        var resolver = new YouTubeResolver(
            NullLogger<YouTubeResolver>.Instance,
            new SingleHandlerFactory(new StubHandler(HttpStatusCode.OK, "{}"))
        );

        Assert.Null(await resolver.ResolvePlaylistAsync("https://example.test/playlist"));
        Assert.Null(
            await resolver.ResolvePlaylistQueryAsync(
                "плейлист",
                5,
                TestContext.Current.CancellationToken
            )
        );
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;

            return Task.FromResult(
                new HttpResponseMessage(status) { Content = new StringContent(body) }
            );
        }
    }
}
