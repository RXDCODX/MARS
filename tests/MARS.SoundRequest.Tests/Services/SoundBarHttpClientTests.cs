using System.Net;
using System.Text.Json;
using MARS.SoundRequest.Services.SoundBarService;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MARS.SoundRequest.Tests.Services;

/// <summary>
/// Обращения к локальному контроллеру звука (MARS.Videos365 или SoundBar).
///
/// Проверяется главное: сервис обращается к нужному адресу и отдаёт в лог отказ
/// вместо исключения. Он зовётся из обработчика команд, и исключение уронило бы
/// разбор команды — зритель получил бы «ошибка» вместо «звук выключен».
///
/// HTTP-подмена своя, а не общая: адреса и тела запросов здесь свои, и смешивать
/// их с заглушкой Spotify значило бы связывать несвязанные тесты.
/// </summary>
public class SoundBarHttpClientTests
{
    private readonly StubHandler _handler = new();
    private readonly SoundBarHttpClient _client = new(
        "http://127.0.0.1:30695",
        Mock.Of<IHttpClientFactory>(),
        NullLogger.Instance
    );

    public SoundBarHttpClientTests()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(instance => instance.CreateClient("Mute Service"))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
        _client = new SoundBarHttpClient(
            "http://127.0.0.1:30695",
            factory.Object,
            NullLogger.Instance
        );
    }

    [Fact]
    public async Task MutePostsToController()
    {
        await _client.Mute("obs64");

        Assert.Equal("http://127.0.0.1:30695/api/soundbar/mute", _handler.LastUrl);
    }

    /// <summary>
    /// Без списка процессов отправляются стандартные: без них контроллер звука
    /// не знает, что заглушить, и вернул бы «отключено» при живом звуке.
    /// </summary>
    [Fact]
    public async Task MuteWithoutProcessesUsesDefaults()
    {
        await _client.Mute();

        using var body = JsonDocument.Parse(_handler.LastBody);
        var processes = body
            .RootElement.GetProperty("ProcessNames")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        Assert.Equal(["obs64", "obs32", "obs-browser-page"], processes);
    }

    [Fact]
    public async Task MuteFailureDoesNotThrow()
    {
        _handler.Respond(HttpStatusCode.InternalServerError, "сломалось");

        await _client.Mute("obs64");
    }

    [Fact]
    public async Task UnmutePostsToController()
    {
        await _client.Unmute();

        Assert.Equal("http://127.0.0.1:30695/api/soundbar/unmute", _handler.LastUrl);
    }

    [Fact]
    public async Task UnmuteFailureDoesNotThrow()
    {
        _handler.Respond(HttpStatusCode.BadGateway, "нет связи");

        await _client.Unmute();
    }

    /// <summary>
    /// Счётчик отложенных запросов читается из ответа контроллера: по нему видно,
    /// сколько запросов ещё можно поставить в очередь.
    /// </summary>
    [Fact]
    public async Task BagCountIsReadFromResponse()
    {
        _handler.Respond(HttpStatusCode.OK, """{"bagCount":"7"}""");

        Assert.Equal("7", await _client.GetBagCount());
    }

    /// <summary>
    /// Ответ без счётчика не должен превращаться в ноль: «0» означал бы «очередь
    /// пуста» вместо «данных нет».
    /// </summary>
    [Fact]
    public async Task MissingBagCountIsReported()
    {
        _handler.Respond(HttpStatusCode.OK, "{}");

        Assert.Equal("No data", await _client.GetBagCount());
    }

    [Fact]
    public async Task BagCountErrorIsReported()
    {
        _handler.Respond(HttpStatusCode.NotFound, "нет");

        Assert.Contains("NotFound", await _client.GetBagCount());
    }

    /// <summary>
    /// Недоступный контроллер не роняет опрос состояния: проверка здоровья
    /// зовётся из цикла плеера и должна просто вернуть «нет».
    /// </summary>
    [Fact]
    public async Task HealthCheckIsFalseWhenControllerIsDown()
    {
        _handler.Respond(HttpStatusCode.ServiceUnavailable, string.Empty);

        Assert.False(await _client.CheckHealthAsync());
    }

    [Fact]
    public async Task HealthCheckIsTrueWhenControllerAnswers()
    {
        _handler.Respond(HttpStatusCode.OK, """{"bagCount":"1"}""");

        Assert.True(await _client.CheckHealthAsync());
    }

    [Fact]
    public async Task HealthCheckIsFalseOnTransportFailure()
    {
        _handler.Throw(new HttpRequestException("соединение отклонено"));

        Assert.False(await _client.CheckHealthAsync());
    }

    /// <summary>
    /// Отмена запроса не должна превращаться в исключение наружу: контроллер звука
    /// на локальной машине регулярно занят.
    /// </summary>
    [Fact]
    public async Task BagCountErrorIsReportedOnTransportFailure()
    {
        _handler.Throw(new HttpRequestException("соединение отклонено"));

        Assert.Contains("соединение отклонено", await _client.GetBagCount());
    }

    [Fact]
    public void DisposeIsSafe()
    {
        _client.Dispose();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string _body = "{}";
        private Exception? _failure;

        public string LastUrl { get; private set; } = string.Empty;

        public string LastBody { get; private set; } = string.Empty;

        public void Respond(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
            _failure = null;
        }

        public void Throw(Exception failure) => _failure = failure;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastUrl = request.RequestUri?.ToString() ?? string.Empty;
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (_failure is not null)
            {
                throw _failure;
            }

            return new HttpResponseMessage(_status) { Content = new StringContent(_body) };
        }
    }
}
