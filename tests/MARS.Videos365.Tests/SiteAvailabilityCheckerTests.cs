using System.Net;
using MARS.Videos365.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Videos365.Tests;

/// <summary>
/// Проверка доступности сайта-источника: результат возвращается, а не бросается,
/// потому что вызывающая сторона (<c>Worker365</c>) на ошибке отправляет
/// уведомление администраторам и корректно завершает проход.
/// </summary>
public class SiteAvailabilityCheckerTests
{
    private static readonly Uri Site = new("https://example.test");

    private sealed class StubDnsResolver : IDnsResolver
    {
        public IPAddress[] Addresses { get; set; } = [IPAddress.Loopback];
        public Exception? Failure { get; set; }
        public string? RequestedHost { get; private set; }

        public Task<IPAddress[]> GetHostAddressesAsync(
            string hostNameOrAddress,
            CancellationToken cancellationToken
        )
        {
            RequestedHost = hostNameOrAddress;

            return Failure is null
                ? Task.FromResult(Addresses)
                : Task.FromException<IPAddress[]>(Failure);
        }
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static (
        SiteAvailabilityChecker Checker,
        StubDnsResolver Dns,
        RecordingHandler Handler
    ) Build(HttpStatusCode status = HttpStatusCode.OK)
    {
        var dns = new StubDnsResolver();
        var handler = new RecordingHandler(status);
        var checker = new SiteAvailabilityChecker(
            dns,
            new SingleClientFactory(new HttpClient(handler)),
            NullLogger<SiteAvailabilityChecker>.Instance
        );

        return (checker, dns, handler);
    }

    [Fact]
    public async Task CheckDnsAsync_ReturnsAddresses_WhenResolverSucceeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, dns, _) = Build();
        dns.Addresses = [IPAddress.Loopback, IPAddress.IPv6Loopback];

        var result = await checker.CheckDnsAsync(Site, ct);

        Assert.True(result.Success);
        Assert.Equal([IPAddress.Loopback, IPAddress.IPv6Loopback], result.Result);
        Assert.Equal("example.test", dns.RequestedHost);
    }

    [Fact]
    public async Task CheckDnsAsync_Fails_WhenResolverThrows()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, dns, _) = Build();
        dns.Failure = new SocketFailureStub();

        var result = await checker.CheckDnsAsync(Site, ct);

        Assert.False(result.Success);
        Assert.Contains("example.test", result.ErrorMessage);
    }

    /// <summary>
    /// Пустой ответ DNS — это «хост не резолвится», а не «резолвер сломался»:
    /// сообщение обязано различать оба случая, иначе в логах стенда обе причины
    /// выглядят одинаково.
    /// </summary>
    [Fact]
    public async Task CheckDnsAsync_Fails_WhenResolverReturnsNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, dns, _) = Build();
        dns.Addresses = [];

        var result = await checker.CheckDnsAsync(Site, ct);

        Assert.False(result.Success);
        Assert.Contains("example.test", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckPingPongAsync_Succeeds_OnSuccessStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, _, handler) = Build();

        var result = await checker.CheckPingPongAsync(Site, ct);

        Assert.True(result.Success);
        Assert.Equal(1, handler.Requests);
    }

    [Fact]
    public async Task CheckPingPongAsync_Fails_AndReportsStatusCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, _, _) = Build(HttpStatusCode.ServiceUnavailable);

        var result = await checker.CheckPingPongAsync(Site, ct);

        Assert.False(result.Success);
        Assert.Contains("503", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAllAsync_Succeeds_WhenDnsAndHttpAreHealthy()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, _, handler) = Build();

        var result = await checker.CheckAllAsync(Site, ct);

        Assert.True(result.Success);
        Assert.Equal(1, handler.Requests);
    }

    /// <summary>
    /// Без DNS HTTP-запрос бессмысленен: источник не разрешается, и обход
    /// избранки всё равно вернёт пустую страницу.
    /// </summary>
    [Fact]
    public async Task CheckAllAsync_SkipsHttp_WhenDnsFails()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, dns, handler) = Build();
        dns.Failure = new SocketFailureStub();

        var result = await checker.CheckAllAsync(Site, ct);

        Assert.False(result.Success);
        Assert.Equal(0, handler.Requests);
    }

    [Fact]
    public async Task CheckAllAsync_Fails_WhenHttpIsUnhealthy()
    {
        var ct = TestContext.Current.CancellationToken;
        var (checker, _, _) = Build(HttpStatusCode.Forbidden);

        var result = await checker.CheckAllAsync(Site, ct);

        Assert.False(result.Success);
        Assert.Contains("403", result.ErrorMessage);
    }

    private sealed class SocketFailureStub : Exception
    {
        public override string Message => "resolver stub failure";
    }
}
