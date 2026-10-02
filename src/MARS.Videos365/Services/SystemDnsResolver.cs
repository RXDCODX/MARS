using System.Net;

namespace MARS.Videos365.Services;

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
