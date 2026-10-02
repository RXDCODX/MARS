using System.Net;
using MARS.Telegram.Services.Booru;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Telegram.Tests;

/// <summary>
/// Выборка постов с Rule34. Сеть подменена: HTTP-обработчик отдаёт заранее
/// заданный ответ.
/// </summary>
public class Rule34RandomPostServiceTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? RequestedUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestedUri = request.RequestUri;
            return Task.FromResult(
                new HttpResponseMessage(status) { Content = new StringContent(body) }
            );
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static (Rule34RandomPostService Service, StubHandler Handler) Build(
        HttpStatusCode status = HttpStatusCode.OK,
        string body = "[]"
    )
    {
        var handler = new StubHandler(status, body);

        return (
            new Rule34RandomPostService(
                NullLogger<Rule34RandomPostService>.Instance,
                new SingleClientFactory(new HttpClient(handler))
            ),
            handler
        );
    }

    private static string PostsJson(params int[] ids) =>
        "["
        + string.Join(",", ids.Select(id => $$"""{"id":{{id}},"file_url":"/f/{{id}}.png"}"""))
        + "]";

    /// <summary>
    /// Забирается сразу 100 записей: DAPI отдаёт страницу фиксированного
    /// размера, и при limit=1 первый элемент всегда был бы одним и тем же постом.
    /// </summary>
    [Fact]
    public async Task GetRandomPostsAsync_AlwaysRequestsTheFullPage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, handler) = Build(body: PostsJson(1));

        await service.GetRandomPostsAsync("waifu", 1, ct);

        Assert.Contains("limit=100", handler.RequestedUri!.Query);
    }

    [Fact]
    public async Task GetRandomPostsAsync_ResolvesTheRequestedCount()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(body: PostsJson(1, 2, 3, 4, 5));

        var result = await service.GetRandomPostsAsync("waifu", 3, ct);

        Assert.True(result.Success);
        Assert.Equal(3, result.Result!.Count);
    }

    [Fact]
    public async Task GetRandomPostsAsync_ReturnsEverything_WhenFewerThanRequested()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(body: PostsJson(1, 2));

        var result = await service.GetRandomPostsAsync("waifu", 5, ct);

        Assert.Equal(2, result.Result!.Count);
    }

    /// <summary>
    /// Случайная выборка возвращается по возрастанию id: иначе состав выдачи
    /// зависел бы от порядка, в котором источник отдал ответ.
    /// </summary>
    [Fact]
    public async Task GetRandomPostsAsync_SortsPickedPostsById()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(body: PostsJson(30, 10, 20));

        var result = await service.GetRandomPostsAsync("waifu", 3, ct);
        var picked = result.Result!;

        Assert.Equal(picked.Select(p => p.Id).Order(), picked.Select(p => p.Id));
    }

    [Fact]
    public async Task GetRandomPostsAsync_MapsSnakeCaseFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(
            body: """[{"id":5,"file_url":"/f/5.png","preview_url":"/p/5.png","owner_id":42}]"""
        );

        var result = await service.GetRandomPostsAsync("waifu", 1, ct);

        var post = result.Result!.Single();
        Assert.Equal(5, post.Id);
        Assert.Equal("/f/5.png", post.FileUrl);
        Assert.Equal("/p/5.png", post.PreviewUrl);
        Assert.Equal(42, post.OwnerId);
    }

    [Fact]
    public async Task GetRandomPostsAsync_Fails_OnErrorStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(HttpStatusCode.ServiceUnavailable, body: PostsJson(1));

        var result = await service.GetRandomPostsAsync("waifu", 1, ct);

        Assert.False(result.Success);
        Assert.Contains("503", result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    public async Task GetRandomPostsAsync_Fails_WhenNothingFound(string body)
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(body: body);

        var result = await service.GetRandomPostsAsync("waifu", 1, ct);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetRandomPostsAsync_Fails_OnUnparsableBody()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build(body: "не json");

        var result = await service.GetRandomPostsAsync("waifu", 1, ct);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetRandomPostsAsync_Fails_ForNonPositiveLimit()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, _) = Build();

        var result = await service.GetRandomPostsAsync("waifu", 0, ct);

        Assert.False(result.Success);
    }

    /// <summary>
    /// Теги уходят в URL-escaped виде: кириллица и пробелы в тегах иначе
    /// разорвали бы запрос.
    /// </summary>
    [Fact]
    public async Task GetRandomPostsAsync_EscapesTagsInTheQuery()
    {
        var ct = TestContext.Current.CancellationToken;
        var (service, handler) = Build();

        await service.GetRandomPostsAsync("waifu girl", 1, ct);

        Assert.Contains("tags=waifu%20girl", handler.RequestedUri!.Query);
    }
}
