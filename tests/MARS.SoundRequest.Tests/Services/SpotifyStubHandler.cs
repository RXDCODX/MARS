using System.Net;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Заглушка ответов Spotify для тестов обёртки плеера: общая на класс, чтобы
/// состояние между тестами не протекало, и сбрасываемая в конструктоте теста.
/// </summary>
internal sealed class SpotifyStubHandler : HttpMessageHandler
{
    public static SpotifyStubHandler Shared { get; } = new();

    public List<RecordedRequest> Requests { get; } = [];

    public string LastUrl => Requests.Count > 0 ? Requests[^1].Url : string.Empty;

    public string LastBody => Requests.Count > 0 ? Requests[^1].Body : string.Empty;

    public void Reset()
    {
        Requests.Clear();
        _body = "{}";
        _status = HttpStatusCode.OK;
    }

    public void Respond(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _body = body;
        _status = status;
    }

    private string _body = "{}";

    private HttpStatusCode _status = HttpStatusCode.OK;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.RequestUri?.ToString() ?? string.Empty, body));

        return new HttpResponseMessage(_status)
        {
            Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json"),
            RequestMessage = request,
        };
    }
}

internal sealed record RecordedRequest(string Url, string Body);
