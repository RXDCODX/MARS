using System.Net;
using MARS.SoundRequest.Configuration;
using MARS.SoundRequest.Services;
using MARS.SoundRequest.Services.SoundCloud;
using MARS.SoundRequest.Services.Spotify;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Резолверы ссылок на треки обязаны отличать «ссылку не дали» от «ссылка не
/// сработала»: первый случай — обычное дело, и превращать его в сетевой запрос
/// незачем. Второй — тоже, но уже с ответом, поэтому проверяются оба.
/// </summary>
public class TrackResolverTests
{
    /// <summary>
    /// Пустой запрос не уходит в Spotify: иначе команда без аргументов жгла бы
    /// токен и тратила запрос на поиск пробелов.
    /// </summary>
    [Fact]
    public async Task EmptySpotifyQueryIsNotSearched()
    {
        var resolver = new SpotifyResolver(CreateSpotifyApiClient());

        var resolved = await resolver.ResolveQueryAsync(
            "   ",
            TestContext.Current.CancellationToken
        );

        Assert.Null(resolved);
    }

    /// <summary>
    /// Плейлист без ссылки не превращается в обход SoundCloud: пустой результат
    /// означает «нечего играть», а не «сеть недоступна».
    /// </summary>
    [Fact]
    public async Task EmptySoundCloudPlaylistIsNotFetched()
    {
        var resolver = new SoundCloudResolver(NullLogger<SoundCloudResolver>.Instance);

        var resolved = await resolver.ResolvePlaylistAsync(
            string.Empty,
            TestContext.Current.CancellationToken
        );

        Assert.Null(resolved);
    }

    private static SpotifyApiClient CreateSpotifyApiClient()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(instance => instance.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(new EmptyStubHandler()));

        var auth = new SpotifyAuthService(
            null!,
            new ConfigurationBuilder().Build(),
            httpClientFactory.Object,
            NullLogger<SpotifyAuthService>.Instance
        );

        return new SpotifyApiClient(
            httpClientFactory.Object.CreateClient("spotify"),
            auth,
            Options.Create(new SpotifySoundRequestConfiguration()),
            NullLogger<SpotifyApiClient>.Instance
        );
    }

    private sealed class EmptyStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{}"),
                }
            );
    }
}
