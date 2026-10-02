using System.Net;
using MARS.CinemaQueue.Configuration;
using MARS.CinemaQueue.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.CinemaQueue.Tests.Services;

/// <summary>
/// Кинопоиск: разбор ответа и извлечение ID из ссылки.
///
/// Проверяется, что ключ API уходит в запрос (без него Кинопоиск отвечает 401 и
/// фильм не находится), и что чужая ссылка не превращается в запрос к
/// Кинопоиску: URL пользователя может быть чем угодно.
/// </summary>
public class KinopoiskServiceTests
{
    private const string MovieJson = """
        {"id":301,"name":"Начало","year":2010,"short_description":"Сон","poster":{"url":"https://img.test/1.jpg"}}
        """;

    [Fact]
    public async Task MovieIsParsedById()
    {
        var handler = new StubHandler(HttpStatusCode.OK, MovieJson);
        var service = Create(handler);

        var movie = await service.GetMovieByIdAsync(301, Token);

        Assert.NotNull(movie);
        Assert.Equal("Начало", movie!.Name);
        Assert.Equal(2010, movie.Year);
    }

    /// <summary>
    /// Ключ API обязателен: без него Кинопоиск отвечает отказом.
    /// </summary>
    [Fact]
    public async Task ApiKeyIsSent()
    {
        var handler = new StubHandler(HttpStatusCode.OK, MovieJson);
        var service = Create(handler);

        await service.GetMovieByIdAsync(301, Token);

        Assert.Equal("test-key", handler.LastApiKey);
    }

    /// <summary>
    /// Ошибка Кинопоиска не роняет очередь: фильм просто остаётся без метаданных.
    /// </summary>
    [Fact]
    public async Task UnavailableApiYieldsNoMovie()
    {
        var service = Create(new StubHandler(HttpStatusCode.Forbidden, "{}"));

        Assert.Null(await service.GetMovieByIdAsync(301, Token));
    }

    [Fact]
    public async Task MovieIsFoundByUrl()
    {
        var handler = new StubHandler(HttpStatusCode.OK, MovieJson);
        var service = Create(handler);

        var movie = await service.GetMovieByUrlAsync("https://www.kinopoisk.ru/film/301", Token);

        Assert.Equal(301, movie!.Id);
        Assert.Equal("https://api.kinopoisk.dev/v1.4/movie/301", handler.LastUrl);
    }

    /// <summary>
    /// Чужая ссылка не приводит ни к какому запросу: идентификатор из неё
    /// выдумывать нельзя.
    /// </summary>
    [Theory]
    [InlineData("https://example.test/film/301")]
    [InlineData("https://www.kinopoisk.ru/person/301")]
    [InlineData("мусор")]
    public async Task ForeignUrlIsRejectedWithoutRequest(string url)
    {
        var handler = new StubHandler(HttpStatusCode.OK, MovieJson);
        var service = Create(handler);

        Assert.Null(await service.GetMovieByUrlAsync(url, Token));
        Assert.Equal(0, handler.Requests);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static KinopoiskService Create(HttpMessageHandler handler) =>
        new(
            new SingleHandlerFactory(handler),
            Options.Create(new KinopoiskConfiguration { Api = "test-key" }),
            NullLogger<KinopoiskService>.Instance
        );

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        public string? LastApiKey { get; private set; }

        public string? LastUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            LastUrl = request.RequestUri?.ToString();
            LastApiKey = request.Headers.TryGetValues("X-API-KEY", out var values)
                ? values.FirstOrDefault()
                : null;

            return Task.FromResult(
                new HttpResponseMessage(status) { Content = new StringContent(body) }
            );
        }
    }
}
