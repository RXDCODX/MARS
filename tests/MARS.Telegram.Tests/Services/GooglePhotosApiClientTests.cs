using System.Net;
using MARS.Shared.Models;
using MARS.Telegram.Services;
using MARS.Telegram.Services.GooglePhotos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Загрузка фото в Google Photos.
///
/// Загрузка двухшаговая: сначала токен загрузки, затем создание элемента медиатеки.
/// Проверяется, что оба шага проходят с действительным токеном и что без
/// авторизации запрос не уходит вовсе.
/// </summary>
public class GooglePhotosApiClientTests
{
    [Fact]
    public async Task PhotoIsUploadedAndTokenReturned()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"uploadToken":"upload-1"}""",
            """{"results":[{"status":{"code":0},"mediaItem":{"id":"media-1"}}]}"""
        );
        var client = Create(handler, accessToken: "access-1");

        var result = await client.UploadPhotoAsync(
            new MemoryStream([1, 2, 3]),
            "photo.jpg",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.Success);
        Assert.Equal("media-1", result.Result);
        Assert.Equal(2, handler.Requests);
    }

    /// <summary>
    /// Токен доступа уходит в заголовке: без него Google отвечает отказом.
    /// </summary>
    [Fact]
    public async Task AccessTokenIsSentInHeader()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"uploadToken":"upload-1"}""",
            """{"results":[{"status":{"code":0},"mediaItem":{"id":"media-1"}}]}"""
        );

        await Create(handler, accessToken: "access-1")
            .UploadPhotoAsync(
                new MemoryStream([1]),
                "photo.jpg",
                TestContext.Current.CancellationToken
            );

        Assert.Equal("Bearer access-1", handler.Authorization);
    }

    /// <summary>
    /// Без действующего токена запрос не уходит: без авторизации Google всё равно
    /// ответил бы отказом.
    /// </summary>
    [Fact]
    public async Task UnauthorizedClientDoesNotCallGoogle()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");

        var result = await Create(handler, accessToken: null)
            .UploadPhotoAsync(
                new MemoryStream([1]),
                "photo.jpg",
                TestContext.Current.CancellationToken
            );

        Assert.False(result.Success);
        Assert.Equal(0, handler.Requests);
    }

    /// <summary>
    /// Отказ Google возвращается как отказ с текстом, а не бросается наружу: загрузка
    /// фото не должна ронять обработчик.
    /// </summary>
    [Fact]
    public async Task GoogleRejectionIsReportedAsFailure()
    {
        var handler = new StubHandler(HttpStatusCode.Forbidden, "denied");

        var result = await Create(handler, accessToken: "access-1")
            .UploadPhotoAsync(
                new MemoryStream([1]),
                "photo.jpg",
                TestContext.Current.CancellationToken
            );

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    private static GooglePhotosApiClient Create(StubHandler handler, string? accessToken)
    {
        var auth = new Mock<IGooglePhotosAuthService>();
        auth.Setup(service => service.GetValidAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        return new GooglePhotosApiClient(
            new SingleHandlerFactory(handler),
            auth.Object,
            NullLogger<GooglePhotosApiClient>.Instance
        );
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>
    /// Ответы Google Photos идут по два запроса, поэтому ответы выдаются по
    /// очереди, а последний повторяется.
    /// </summary>
    private sealed class StubHandler(HttpStatusCode status, string body, string? secondBody = null)
        : HttpMessageHandler
    {
        private int _calls;

        public int Requests { get; private set; }

        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            Authorization = request.Headers.Authorization?.ToString();
            var content = _calls++ == 0 ? body : secondBody ?? body;

            return Task.FromResult(
                new HttpResponseMessage(status) { Content = new StringContent(content) }
            );
        }
    }
}
