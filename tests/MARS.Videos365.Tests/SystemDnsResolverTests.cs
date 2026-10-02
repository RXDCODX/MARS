using System.Net;
using MARS.Videos365.Services;

namespace MARS.Videos365.Tests;

/// <summary>
/// <see cref="SystemDnsResolver"/> — единственная реализация
/// <see cref="IDnsResolver"/>. Проверяется на IP-литерале: резолв литерала
/// выполняется без обращения к DNS-серверу, поэтому тест не ходит в сеть.
/// </summary>
public class SystemDnsResolverTests
{
    [Fact]
    public async Task GetHostAddressesAsync_ReturnsTheSameAddress_ForIpLiteral()
    {
        var ct = TestContext.Current.CancellationToken;
        var resolver = new SystemDnsResolver();

        var addresses = await resolver.GetHostAddressesAsync("127.0.0.1", ct);

        Assert.Contains(IPAddress.Loopback, addresses);
    }

    [Fact]
    public void SystemDnsResolver_ImplementsTheResolverAbstraction()
    {
        var result = new SystemDnsResolver() is IDnsResolver;

        Assert.True(result);
    }
}
