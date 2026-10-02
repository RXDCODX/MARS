using System.Net;

namespace MARS.Videos365.Services;

/// <summary>
/// Резолвер DNS для проверки доступности сайта-источника.
/// </summary>
public interface IDnsResolver
{
    Task<IPAddress[]> GetHostAddressesAsync(
        string hostNameOrAddress,
        CancellationToken cancellationToken
    );
}
