using System.Net;
using MARS.Videos365.Application.Ports;

namespace MARS.Videos365.Infrastructure.Services;

/// <summary>
/// Резолвер поверх системного DNS.
/// </summary>
public sealed class SystemDnsResolver : IDnsResolver
{
    public Task<IPAddress[]> GetHostAddressesAsync(
        string hostNameOrAddress,
        CancellationToken cancellationToken
    )
    {
        return Dns.GetHostAddressesAsync(hostNameOrAddress, cancellationToken);
    }
}
